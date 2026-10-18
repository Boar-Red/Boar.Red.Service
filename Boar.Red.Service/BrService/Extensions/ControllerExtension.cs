using BaoXia.Utils.Extensions;
using BaoXia.Utils.Models;
using Boar.Red.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Boar.Red.Service.BrService.Extensions;

public static class ControllerExtension
{
	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static bool IsInBrServiceRequestPipe(this Controller controller)
	{
		if (controller == null)
		{
			return false;
		}
		if (controller.HttpContext == null)
		{
			return true;
		}
		return false;
	}

	public static ClientIpInfo GetClientIpInfoWithRequest(this Controller controller, RequestWithClientIpInfo requestWithClientIpInfo)
	{
		if (IsInBrServiceRequestPipe(controller))
		{
			return requestWithClientIpInfo.ClientIpInfo;
		}
		return controller.GetClientIpInfo();
	}

	#endregion
}
