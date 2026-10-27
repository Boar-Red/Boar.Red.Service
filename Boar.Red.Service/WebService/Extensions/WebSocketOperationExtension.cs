using BaoXia.Utils.Extensions;
using System.Net.WebSockets;

namespace Boar.Red.Service.WebService.Extensions;

public static class WebSocketOperationExtension
{
	////////////////////////////////////////////////
	// @静态常量
	////////////////////////////////////////////////

	#region 静态常量

	public const double SendTimeoutSecondsDefault = 30.0;

	public const double CloseTimeoutSecondsDefault = 5.0;

	#endregion


	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static async Task ExecuteWithTimeoutAsync(this WebSocket webSocket, Func<CancellationToken, Task> operation,
		double timeoutSeconds, CancellationToken cancellationToken)
	{
		if (!double.IsFinite(timeoutSeconds) || timeoutSeconds <= 0)
		{
			timeoutSeconds = SendTimeoutSecondsDefault;
		}
		using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
		try
		{
			// WaitAsync 同时限制不响应取消令牌的操作；失败后中断底层连接，停止保活及挂起的网络操作。
			await operation(timeoutSource.Token).WaitAsync(timeoutSource.Token);
		}
		catch
		{
			webSocket.Abort();
			throw;
		}
	}

	public static Task SendWithTimeoutAsync(this WebSocket webSocket, ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType,
		bool endOfMessage, double timeoutSeconds, CancellationToken cancellationToken)
	{
		return webSocket.ExecuteWithTimeoutAsync(token => webSocket.SendAsync(buffer, messageType, endOfMessage, token).AsTask(),
			timeoutSeconds, cancellationToken);
	}

	public static Task CloseWithTimeoutAsync(this WebSocket webSocket, WebSocketCloseStatus closeStatus, string? statusDescription,
		double timeoutSeconds, CancellationToken cancellationToken)
	{
		if (!double.IsFinite(timeoutSeconds) || timeoutSeconds <= 0)
		{
			timeoutSeconds = CloseTimeoutSecondsDefault;
		}
		return webSocket.ExecuteWithTimeoutAsync(token => webSocket.TryToCloseAsync(closeStatus, statusDescription, token),
			timeoutSeconds, cancellationToken);
	}

	#endregion
}
