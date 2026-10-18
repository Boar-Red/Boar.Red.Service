using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.Extensions;
using Boar.Red.Service.BrService.Interfaces;
using Boar.Red.Service.ViewModels;

namespace Boar.Red.Service.Extensions;

public static class IWebSocketServiceExtension
{
	////////////////////////////////////////////////
	// @类方法：返回带有跟踪信息的Json响应。
	////////////////////////////////////////////////

	#region 类方法：返回带有跟踪信息的Json响应。

	//public static JsonResult JsonWithTracable(this IWebSocketService webSocketService, ITraceable traceable, object responseObject)
	//{
	//	if (responseObject is ITraceable traceableResponse)
	//	{
	//		//
	//		traceableResponse.SetTraceable(traceable);
	//		//
	//	}
	//	return webSocketService.Json(responseObject);
	//}

	#endregion


	////////////////////////////////////////////////
	// @类方法：从【HttpHeader】中获取客户端信息。
	////////////////////////////////////////////////

	#region 类方法

	public static BrApiClientInfo GetClientInfo(this IWebSocketService webSocketService)
	{
		var clientInfo = webSocketService.HttpContext.Request.Headers.GetClientInfo(webSocketService.CustomHttpHeaders);
		if (webSocketService.GetClientIpInfo() is { } clientIpInfo)
		{
			clientInfo.ConnectionIpEndPointsString = clientIpInfo.ConnectionIpEndPointsString;
			clientInfo.KeyIpAddress = clientIpInfo.KeyIpAddress;
		}
		return clientInfo;
	}

	#endregion


	////////////////////////////////////////////////
	// @类方法：从【HttpContext】中获取令牌检查结果信息。
	////////////////////////////////////////////////

	#region 类方法

	public static TokenCheckResultInfo? GetTokenCheckResultInfo(this IWebSocketService webSocketService)
	{
		return webSocketService.HttpContext.GetTokenCheckResultInfo();
	}

	#endregion
}