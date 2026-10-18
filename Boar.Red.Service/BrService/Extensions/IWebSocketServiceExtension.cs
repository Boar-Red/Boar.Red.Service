using BaoXia.Utils.Extensions;
using BaoXia.Utils.Models;
using Boar.Red.Service.BrService.Interfaces;
using Boar.Red.Service.ViewModels;

namespace Boar.Red.Service.BrService.Extensions;

public static class IWebSocketServiceExtension
{
	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

#pragma warning disable IDE0060 // 删除未使用的参数
	public static bool IsInBrServiceRequestPipe(this IWebSocketService webSocketService)
#pragma warning restore IDE0060 // 删除未使用的参数
	{
		return false;
	}

	/// <summary>
	/// 获取客户端的Ip信息。
	/// </summary>
	/// <param name="webSocketService">当前控制器对象。</param>
	/// <returns>当前控制器对象的客户端Ip信息。</returns>
	public static ClientIpInfo GetClientIpInfo(this IWebSocketService webSocketService)
	{
		if (webSocketService == null)
		{
			return new ClientIpInfo();
		}
		return webSocketService.HttpContext.Request.GetClientIpInfo();
	}

	public static ClientIpInfo GetClientIpInfoWithRequest(this IWebSocketService webSocketService, RequestWithClientIpInfo requestWithClientIpInfo)
	{
		if (IsInBrServiceRequestPipe(webSocketService))
		{
			return requestWithClientIpInfo.ClientIpInfo;
		}
		return webSocketService.GetClientIpInfo();
	}

	#endregion
}
