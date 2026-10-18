using BaoXia.Constants.Interfaces;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Models;
using Boar.Red.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Boar.Red.Service.Extensions;

public static class ControllerExtension
{
	////////////////////////////////////////////////
	// @类方法：返回带有跟踪信息的Json响应。
	////////////////////////////////////////////////

	#region 类方法：返回带有跟踪信息的Json响应。

	public static JsonResult JsonWithTracable(this Controller controller, ITraceable traceable, object responseObject)
	{
		if (responseObject is ITraceable traceableResponse)
		{
			//
			traceableResponse.SetTraceable(traceable);
			//
		}
		return controller.Json(responseObject);
	}

	#endregion


	////////////////////////////////////////////////
	// @类方法：从【HttpHeader】中获取客户端信息。
	////////////////////////////////////////////////

	#region 类方法

	public static BrApiClientInfo GetClientInfo(this Controller controller)
	{
		var clientInfo = controller.HttpContext.Request.Headers.GetClientInfo();
		if (controller.GetClientIpInfo() is ClientIpInfo clientIpInfo)
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

	public static TokenCheckResultInfo? GetTokenCheckResultInfo(this Controller controller)
	{
		return controller.HttpContext.GetTokenCheckResultInfo();
	}

	#endregion
}