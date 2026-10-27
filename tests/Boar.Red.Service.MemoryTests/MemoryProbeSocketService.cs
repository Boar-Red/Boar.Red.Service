using Boar.Red.Service.BrService.Services;
using Boar.Red.Service.BrService.ViewModels;

namespace Boar.Red.Service.MemoryTests;

public class MemoryProbeSocketService() : WebSocketService<WebSocketServiceRequest, WebSocketServiceResponse>("/memory-probe")
{
	////////////////////////////////////////////////
	// @重载
	////////////////////////////////////////////////

	#region 重载

	protected override Task<WebSocketServiceResponseInfo<WebSocketServiceResponse>?> DidProcessRequestAsync(WebSocketServiceRequest? requestParam)
	{
		return Task.FromResult<WebSocketServiceResponseInfo<WebSocketServiceResponse>?>(null);
	}

	#endregion
}
