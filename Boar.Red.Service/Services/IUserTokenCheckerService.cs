using Boar.Red.Service.BrService.Interfaces;
using Boar.Red.Service.BrService.ViewModels;
using Boar.Red.Service.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Boar.Red.Service.Services;

/// <summary>
/// 令牌检查器服务接口。
/// </summary>
public interface IUserTokenCheckerService
{
	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public Task<TokenCheckResultInfo?> CheckTokenInHttpContextAsync(
		HttpContext httpContext, ActionExecutingContext? actionExecutingContext,
		IWebSocketService? webSocketService, WebSocketServiceRequest? webSocketServiceRequest);

	#endregion
}