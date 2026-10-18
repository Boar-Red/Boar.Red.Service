using BaoXia.Constants;
using BaoXia.Constants.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using System.Net.Sockets;

namespace Boar.Red.Service.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class LanAccessibleAttribute : Attribute, IActionFilter
{
	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	private static bool IsLanIpAddress(string? ipAddressString)
	{
		if (ipAddressString?.Equals("LOCALHOST", StringComparison.OrdinalIgnoreCase) == true)
		{
			return true;
		}
		if (!IPAddress.TryParse(ipAddressString, out var ipAddress))
		{
			return false;
		}

		if (IPAddress.IsLoopback(ipAddress))
		{
			return true;
		}

		if (ipAddress.IsIPv4MappedToIPv6)
		{
			ipAddress = ipAddress.MapToIPv4();
		}

		var addressBytes = ipAddress.GetAddressBytes();
		if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
		{
			return addressBytes[0] == 10
				|| (addressBytes[0] == 172
					&& addressBytes[1] >= 16
					&& addressBytes[1] <= 31)
				|| (addressBytes[0] == 192
					&& addressBytes[1] == 168)
				|| (addressBytes[0] == 169
					&& addressBytes[1] == 254);
		}

		if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
		{
			return ipAddress.IsIPv6LinkLocal
				|| (addressBytes[0] & 0xFE) == 0xFC;
		}

		return false;
	}

	#endregion


	////////////////////////////////////////////////
	// @重载
	////////////////////////////////////////////////

	#region 重载

	public void OnActionExecuting(ActionExecutingContext context)
	{
		var remoteIpAddress = context.HttpContext.Connection.RemoteIpAddress;
		if (remoteIpAddress == null
			|| !IsLanIpAddress(remoteIpAddress.ToString()))
		{
			context.Result = new JsonResult(new Response()
			{
				Error = Error.ClientAddressInvalid,
				ErrorDescription = "只有内网才可访问。"
			})
			{
				StatusCode = StatusCodes.Status403Forbidden
			};
		}
	}

	public void OnActionExecuted(ActionExecutedContext context)
	{
	}

	#endregion
}
