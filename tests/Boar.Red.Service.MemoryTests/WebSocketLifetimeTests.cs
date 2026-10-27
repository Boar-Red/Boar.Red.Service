using Boar.Red.Service.BrService.Constants;
using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.BrService.ViewModels;
using Boar.Red.Service.WebService.Extensions;
using Boar.Red.Service.WebService.Models;
using BaoXia.Utils.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Threading.Channels;

namespace Boar.Red.Service.MemoryTests;

public static class WebSocketLifetimeTests
{
	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static async Task RunAsync()
	{
		using var blockedSend = new ProbeWebSocket { BlockSend = true };
		await ExpectCancellationAsync(blockedSend.SendWithTimeoutAsync(new byte[] { 1 }, WebSocketMessageType.Binary, true, 0.05, CancellationToken.None));
		Check(blockedSend.AbortCount == 1, "A send that ignores its cancellation token must still be aborted on timeout.");
		using var blockedClose = new ProbeWebSocket { BlockClose = true };
		await ExpectCancellationAsync(blockedClose.CloseWithTimeoutAsync(WebSocketCloseStatus.NormalClosure, "test", 0.05, CancellationToken.None));
		Check(blockedClose.AbortCount == 1, "A peer that never completes the close handshake must be aborted on timeout.");
		using var cancelledSend = new ProbeWebSocket { BlockSend = true };
		using var cancelSource = new CancellationTokenSource();
		var send = cancelledSend.SendWithTimeoutAsync(new byte[] { 1 }, WebSocketMessageType.Binary, true, 30, cancelSource.Token);
		cancelSource.Cancel();
		await ExpectCancellationAsync(send);
		Check(cancelledSend.AbortCount == 1, "Caller cancellation must abort the pending network operation.");
		using var successful = new ProbeWebSocket();
		await successful.SendWithTimeoutAsync(new byte[] { 1 }, WebSocketMessageType.Binary, true, 0.5, CancellationToken.None);
		await successful.CloseWithTimeoutAsync(WebSocketCloseStatus.NormalClosure, "test", 0.5, CancellationToken.None);
		Check(successful.AbortCount == 0 && successful.State == WebSocketState.Closed, "Successful sends and closes must keep normal behavior.");
		Console.WriteLine("PASS bounded sends and close handshakes, non-cooperative operations, caller cancellation and normal completion");
		await TestServiceSendPathsAsync();
		await TestKeepAliveAsync();
	}

	private static async Task TestServiceSendPathsAsync()
	{
		var config = new MemoryTestConfig();
		config.WebServiceConfig.WebSocketSendTimeoutSeconds = 0.05;
		config.WebServiceConfig.WebSocketCloseTimeoutSeconds = 0.05;
		var middleware = new MemoryTestMiddleware(config);
		using var queuedSocket = new ProbeWebSocket { BlockSend = true };
		var connection = new ClientConnectionInfo(1, new ApplicationInfo { ApplicationIdentity = "test", AesKey = string.Empty });
		Check(connection.TryAddSocket(queuedSocket, new ClientIpInfo(), out _), "The response queue must have a connected socket.");
		var response = new ApiResponseInfo<object>("test-response")
		{
			Result = new { Value = "test" }, ApiResponseSentCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously)
		};
		var sent = await connection.SendApiResponseInfoToSendToClientAsync(response, middleware, config.BrServiceConfig,
			DataPackageEncryptionMethod.None, string.Empty).WaitAsync(TimeSpan.FromSeconds(2));
		Check(!sent && queuedSocket.AbortCount == 1, "A queued response must report failure and abort a blocked socket within the configured timeout.");
		connection.RemoveSocket(queuedSocket);
		connection.CancellationTokenSource.Dispose();

		using var serviceSocket = new ProbeWebSocket { BlockSend = true };
		var service = new MemoryProbeSocketService();
		service.InitializeWithBrServiceMiddleware(middleware, new DefaultHttpContext(), serviceSocket);
		await ExpectCancellationAsync(service.SendStringAsync("test"));
		Check(serviceSocket.AbortCount == 1, "The generic WebSocket service must apply the configured send timeout.");
		using var closingSocket = new ProbeWebSocket { BlockClose = true };
		var closingService = new MemoryProbeSocketService();
		var context = new DefaultHttpContext();
		closingService.InitializeWithBrServiceMiddleware(middleware, context, closingSocket);
		await ExpectCancellationAsync(closingService.DiscountWebSocketInHttpContextAsync(context, closingSocket,
			new WebSocketServiceSocketCloseInfo { WebSocketCloseStatus = WebSocketCloseStatus.NormalClosure }));
		Check(closingSocket.AbortCount == 1, "The generic WebSocket service must bound its close handshake.");
		Console.WriteLine("PASS configured timeouts for queued business responses and generic WebSocket service sends and closure");
	}

	private static async Task TestKeepAliveAsync()
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Logging.ClearProviders();
		await using var app = builder.Build();
		var accepted = Channel.CreateUnbounded<WebSocket>();
		var completed = Channel.CreateUnbounded<WebSocketState>();
		app.UseWebSocketService(new WebServiceConfig { WebSocketKeepAliveIntervalSeconds = 0.1, WebSocketKeepAliveTimeoutSeconds = 0.2 });
		app.Run(async context =>
		{
			using var socket = await context.WebSockets.AcceptWebSocketAsync();
			await accepted.Writer.WriteAsync(socket);
			try
			{
				await socket.ReceiveAsync(new ArraySegment<byte>(new byte[256]), context.RequestAborted);
			}
			catch (WebSocketException)
			{
				// The checks below distinguish expected peer termination from a keep-alive timeout.
			}
			catch (OperationCanceledException)
			{
				// Host shutdown or an aborted peer may cancel the pending receive.
			}
			finally
			{
				await completed.Writer.WriteAsync(socket.State);
			}
		});
		await app.StartAsync();
		try
		{
			var uri = new Uri(app.Urls.Single().Replace("http://", "ws://") + "/keep-alive");
			using (var healthy = new ClientWebSocket())
			{
				await healthy.ConnectAsync(uri, CancellationToken.None);
				var serverSocket = await accepted.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
				var receive = healthy.ReceiveAsync(new ArraySegment<byte>(new byte[256]), CancellationToken.None);
				await Task.Delay(650);
				Check(serverSocket.State == WebSocketState.Open && healthy.State == WebSocketState.Open && !receive.IsCompleted,
					"An idle peer processing Ping/Pong frames must remain connected across multiple keep-alive periods.");
				healthy.Abort();
				try
				{
					await receive.WaitAsync(TimeSpan.FromSeconds(5));
				}
				catch (WebSocketException)
				{
				}
				catch (OperationCanceledException)
				{
				}
				await completed.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
			}
			using var unresponsive = new ClientWebSocket();
			await unresponsive.ConnectAsync(uri, CancellationToken.None);
			await accepted.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
			// ClientWebSocket handles control frames while ReceiveAsync is pending. Deliberately omit it here.
			var state = await completed.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
			Check(state == WebSocketState.Aborted, "A peer that never processes a Ping must be aborted instead of accumulating keep-alive tasks.");
			Console.WriteLine("PASS real Kestrel WebSockets: responsive idle connection survives and unresponsive connection is aborted by Ping/Pong timeout");
		}
		finally
		{
			using var stopSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			await app.StopAsync(stopSource.Token);
		}
	}

	private static async Task ExpectCancellationAsync(Task operation)
	{
		try
		{
			await operation.WaitAsync(TimeSpan.FromSeconds(2));
			throw new InvalidOperationException("The operation should have been cancelled.");
		}
		catch (OperationCanceledException)
		{
		}
	}

	private static void Check(bool condition, string description)
	{
		if (!condition)
		{
			throw new InvalidOperationException(description);
		}
	}

	#endregion
}
