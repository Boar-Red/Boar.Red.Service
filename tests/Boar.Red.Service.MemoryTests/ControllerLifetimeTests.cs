using BaoXia.Constants;
using BaoXia.Constants.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Boar.Red.Service.MemoryTests;

public static class ControllerLifetimeTests
{
	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static async Task RunAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
		builder.Logging.ClearProviders();
		builder.Host.UseDefaultServiceProvider(options => { options.ValidateScopes = true; options.ValidateOnBuild = true; });
		builder.Services.AddScoped<ScopedProbe>();
		builder.Services.AddScoped<AsyncScopedProbe>();
		builder.Services.AddTransient<MemoryProbeController>();
		builder.Services.AddTransient<ConstructionFailureController>();
		await using var app = builder.Build();
		var previousApplication = BaoXia.Utils.Environment.ApplicationBuilder;
		BaoXia.Utils.Environment.ApplicationBuilder = app;
		try
		{
			var config = new MemoryTestConfig();
			config.WebServiceConfig.WebSocketSendTimeoutSeconds = 0.05;
			var middleware = new MemoryTestMiddleware(config);
			using var socket = new ProbeWebSocket
			{
				BeforeSend = () => Check(ScopedProbe.Created > ScopedProbe.Disposed && AsyncScopedProbe.Created > AsyncScopedProbe.Disposed,
					"The controller scope must remain active until the response has been serialized and sent.")
			};
			for (var index = 0; index < 1000; index++)
			{
				var response = await middleware.InvokeRequestAsync("/memoryProbe/success", socket);
				Check(response?.StatusCode == 200, "Asynchronous controller requests must succeed with scoped dependencies.");
				Check(ScopedProbe.Created == ScopedProbe.Disposed && AsyncScopedProbe.Created == AsyncScopedProbe.Disposed,
					"Every request must await disposal of synchronous and asynchronous scoped dependencies.");
			}
			Check(MemoryProbeController.Disposed == 1000, "Controllers must be disposed exactly once.");
			var failed = await middleware.InvokeRequestAsync("/memoryProbe/failure", socket);
			Check(failed?.ResultInObject is Response { Error: Error.ProgramError }, "Controller exceptions must still produce the existing business error response.");
			Check(ScopedProbe.Created == ScopedProbe.Disposed && AsyncScopedProbe.Created == AsyncScopedProbe.Disposed,
				"An action failure must release its scope.");
			var redirected = await middleware.InvokeRequestAsync("/memoryProbe/redirectOnce", socket);
			Check(redirected?.StatusCode == 200, "A redirect must invoke the target action and finish.");
			var cycle = await middleware.InvokeRequestAsync("/memoryProbe/redirectCycle", socket);
			Check(cycle?.StatusCode == 508, "A cyclic redirect must stop instead of retaining unlimited controller scopes.");
			Check(ScopedProbe.Created == ScopedProbe.Disposed && AsyncScopedProbe.Created == AsyncScopedProbe.Disposed,
				"Redirect targets must release their scopes.");
			Check(MemoryProbeController.Disposed == ScopedProbe.Created, "No controller may be disposed twice after redirects or failures.");

			using var blockedSocket = new ProbeWebSocket { BlockSend = true };
			try
			{
				await middleware.InvokeRequestAsync("/memoryProbe/success", blockedSocket);
				throw new InvalidOperationException("A blocked response send must time out.");
			}
			catch (OperationCanceledException)
			{
				Check(blockedSocket.AbortCount == 1, "A timed out response must abort its WebSocket.");
			}
			Check(ScopedProbe.Created == ScopedProbe.Disposed && AsyncScopedProbe.Created == AsyncScopedProbe.Disposed,
				"Sending failures must release the controller and its asynchronous dependencies.");

			var beforeExternalScope = ScopedProbe.Disposed;
			await using (var suppliedRoute = middleware.Resolve("/memoryProbe/success"))
			{
				Check(suppliedRoute != null, "The existing synchronous resolver must remain usable.");
				await middleware.InvokeRequestAsync("/memoryProbe/success", socket, suppliedRoute);
				Check(ScopedProbe.Disposed == beforeExternalScope, "A supplied route must remain owned by its caller.");
			}
			Check(ScopedProbe.Disposed == beforeExternalScope + 1, "The supplied route must release its scope when its owner finishes.");
			var constructionFailure = await middleware.InvokeRequestAsync("/constructionFailure/success", new ProbeWebSocket());
			Check(constructionFailure?.StatusCode == 500, "Controller construction errors must produce a failure response.");
			Check(ScopedProbe.Created == ScopedProbe.Disposed && AsyncScopedProbe.Created == AsyncScopedProbe.Disposed,
				"Partially constructed controllers must not leak asynchronous scoped dependencies.");

			ForceCollection();
			Check(MemoryProbeController.References.Count(reference => reference.IsAlive) < 3,
				"Completed controllers must be collectible while the root service provider is still alive.");
			Check(ScopedProbe.References.Count(reference => reference.IsAlive) < 3,
				"Scoped dependencies must not accumulate in the root provider.");
			Console.WriteLine("PASS 1000 calls, controller collection, async scope disposal, failures, redirects, send timeouts and supplied route ownership");
		}
		finally
		{
			BaoXia.Utils.Environment.ApplicationBuilder = previousApplication;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ForceCollection()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
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
