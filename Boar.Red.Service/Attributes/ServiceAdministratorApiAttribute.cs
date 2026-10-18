using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils.Extensions;
using Boar.Red.Service.Constants;
using Boar.Red.Service.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Boar.Red.Service.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ServiceAdministratorApiAttribute : Attribute, IActionFilter
{
	////////////////////////////////////////////////
	// @实现“IActionFilter”
	////////////////////////////////////////////////

	#region 实现“IActionFilter”

	public void OnActionExecuting(ActionExecutingContext context)
	{
		var httpContext = context.HttpContext;

		var adminstratorSessionService = BaoXia.Utils.Environment.GetService<ServiceAdministratorService>();
		if (adminstratorSessionService == null)
		{
			System.Diagnostics.Debug.WriteLine("管理员会话信息检查失败，没有发现“ServiceAdministratorService”服务。");

			// !!! WebSocket 握手完成后，响应的状态码与头部信息已不可修改，
			//     此时仅设置“Result”，由调用方负责把校验失败的结果发送给客户端。 !!!
			if (httpContext.Response.HasStarted != true)
			{
				httpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
			}
			context.Result = new JsonResult(new Response()
			{
				Error = Error.UserTokenNeedPermission
			});
			return;
		}

		var administratorSession = adminstratorSessionService.GetSessionWithRequest(
		    httpContext.Request, out var httpStatusCode, out var error, out var errorDescription);

		if (administratorSession != null)
		{
			httpContext.Items.Remove(HttpContextItemKeys.ServiceAdministrator.SessionObject);
			httpContext.Items.Add(HttpContextItemKeys.ServiceAdministrator.SessionObject, administratorSession);
		}
		else
		{
			// !!! WebSocket 握手完成后，响应的状态码与头部信息已不可修改，
			//     此时仅设置“Result”，由调用方负责把校验失败的结果发送给客户端。 !!!
			if (httpContext.Response.HasStarted != true)
			{
				httpContext.Response.StatusCode = (int)httpStatusCode;
				httpContext.Response.Headers.Append(
				    HttpHeaderKeys.ServiceAdministrator.ClientIpAddress,
				    httpContext.Request.GetClientConnectionAddressInfesString());
			}
			context.Result = new JsonResult(new Response()
			{
				Error = error,
				ErrorDescription = errorDescription
			});
			return;
		}
	}

	public void OnActionExecuted(ActionExecutedContext context)
	{ }

	#endregion
}
