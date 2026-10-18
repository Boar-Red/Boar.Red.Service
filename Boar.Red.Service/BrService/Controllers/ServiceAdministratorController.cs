using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Models;
using Boar.Red.Service.Attributes;
using Boar.Red.Service.BrService.Extensions;
using Boar.Red.Service.BrService.ViewModels;
using Boar.Red.Service.Constants;
using Boar.Red.Service.Models;
using Boar.Red.Service.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Boar.Red.Service.BrService.Controllers;

public abstract class ServiceAdministratorController(
	ServiceAdministratorService serviceAdministratorService, bool isAdministratorSessionTokenHttpOnly = true) : Controller
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	private readonly ServiceAdministratorService _serviceAdministratorService = serviceAdministratorService;

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	[ApiName("服务管理员登录")]
	[HttpPost]
	public IActionResult Login([FromBody] ServiceAdministratorLoginRequest request)
	{
		var isInBrServiceRequestPipe = this.IsInBrServiceRequestPipe();
		ConnectionIpEndPoints? connectionIpEndPoints = null;
		if (isInBrServiceRequestPipe)
		{
			if (request.ClientIpInfo is not { } clientIpInfo)
			{
				return Json(new ServiceAdministratorLoginResponse()
				{
					Error = Error.OperationFailed,
					ErrorDescription = $"请求参数无效，“{nameof(request.ClientIpInfo)}”字段为“null”。"
				});
			}

			connectionIpEndPoints = clientIpInfo.ToConnectionIpEndPoints();
		}
		else
		{
			connectionIpEndPoints = Request.GetConnectionIpEndPoints();
		}

		var response = new ServiceAdministratorLoginResponse
		{
			Error = _serviceAdministratorService.Login(request.Account, request.Password, connectionIpEndPoints, out var administratorSession)
		};
		if (administratorSession != null)
		{
			//
			response.SessionToken = administratorSession.Token;
			//

			if (!isInBrServiceRequestPipe)
			{
				Response.Cookies.Append(CookieNames.ServiceAdministrator.SessionToken, administratorSession.Token,
				    new CookieOptions()
				    {
					    Path = "/",
					    Secure = true,
					    HttpOnly = isAdministratorSessionTokenHttpOnly,
#if DEBUG
					    SameSite = SameSiteMode.None
#else
						SameSite = SameSiteMode.Strict
#endif
				    });
			}
		}
		return Json(response);
	}

	[ApiName("服务管理员登出")]
	[HttpPost]
	[ServiceAdministratorApi]
	public IActionResult Logout([FromBody] ServiceAdministratorRequest? request)
	{
		var isInBrServiceRequestPipe = this.IsInBrServiceRequestPipe();
		ServiceAdministratorSession? currrentAdministratorSession;
		if (isInBrServiceRequestPipe)
		{
			if (request == null)
			{
				return Json(new ServiceAdministratorLoginResponse()
				{
					Error = Error.OperationFailed,
					ErrorDescription = $"请求参数无效，请求参数为“null”。"
				});
			}
			if (request.ClientUserId == 0)
			{
				return Json(new ServiceAdministratorLoginResponse()
				{
					Error = Error.OperationFailed,
					ErrorDescription = $"请求参数无效，“{nameof(request.ClientUserId)}”字段为“0”。"
				});
			}
			if (request.ClientIpInfo == null)
			{
				return Json(new ServiceAdministratorLoginResponse()
				{
					Error = Error.OperationFailed,
					ErrorDescription = $"请求参数无效，“{nameof(request.ClientIpInfo)}”字段为“null”。"
				});
			}

			// !!!
			currrentAdministratorSession = _serviceAdministratorService.GetSessionWithToken(request.ServiceAdministratorSessionToken);
			// !!!
		}
		else
		{
			currrentAdministratorSession
				= HttpContext.Items[HttpContextItemKeys.ServiceAdministrator.SessionObject] as ServiceAdministratorSession;
		}
		if (currrentAdministratorSession == null)
		{
			return Json(new Response()
			{
				Error = Error.UserTokenNotExisted,
				ErrorDescription = "无法获取到当前管理员会话信息。"
			});
		}

		var response = new Response
		{
			Error = _serviceAdministratorService.Logout(currrentAdministratorSession.Token)
		};
		//
		Response.Cookies.Append(CookieNames.ServiceAdministrator.SessionToken, currrentAdministratorSession.Token,
			new CookieOptions()
			{
				Path = "/",
				Secure = true,
				HttpOnly = isAdministratorSessionTokenHttpOnly,
				Expires = DateTimeOffset.Now.AddHours(-1),
#if DEBUG
				SameSite = SameSiteMode.None
#else
				SameSite = SameSiteMode.Strict
#endif
			});
		//
		return Json(response);
	}

	[ApiName("验证当前管理员连接")]
	[ServiceAdministratorApi]
	public IActionResult IsServiceAdministratorTokenValid([FromBody] ServiceAdministratorRequest? request)
	{
		var isInBrServiceRequestPipe = this.IsInBrServiceRequestPipe();
		if (!isInBrServiceRequestPipe)
		{
			return new OkResult();
		}

		if (request == null)
		{
			return Json(new Response()
			{
				Error = Error.OperationFailed,
				ErrorDescription = $"请求参数无效，请求参数为“null”。"
			});
		}

		var getResult = _serviceAdministratorService.GetValidSessionWithToken(
			request.ServiceAdministratorSessionToken, request.ClientIpInfo?.KeyIpAddress);
		{ }
		return Json(new Response(getResult));
	}

	#endregion
}
