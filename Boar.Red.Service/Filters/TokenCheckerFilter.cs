using BaoXia.Constants;
using BaoXia.Constants.Models;
using Boar.Red.Service.BrService.Interfaces;
using Boar.Red.Service.BrService.ViewModels;
using Boar.Red.Service.Constants;
using Boar.Red.Service.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Boar.Red.Service.Filters;

public class TokenCheckerFilter : IAsyncActionFilter
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	private IUserTokenCheckerService? _tokenCheckerService;

	private bool _isTokenCheckerServiceGot;

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public async Task<Response?> CheckRequestTokenForWebSocketServiceInHttpContextAsync(
		HttpContext httpContext, IWebSocketService webSocketService, WebSocketServiceRequest? webSocketServiceRequest)
	{
		if (!_isTokenCheckerServiceGot)
		{
			_tokenCheckerService ??= BaoXia.Utils.Environment.GetService<IUserTokenCheckerService>();
			_isTokenCheckerServiceGot = true;
		}
		if (_tokenCheckerService == null)
		{
			return null;
		}

		var tokenCheckResultInfo = await _tokenCheckerService.CheckTokenInHttpContextAsync(
			httpContext, null, webSocketService, webSocketServiceRequest);
		//if (tokenCheckResultInfo == null)
		//{
		//	////////////////////////////////////////////////
		//	throw new ArgumentNullException(
		//		nameof(tokenCheckResultInfo),
		//		"无法从令牌检查服务中，获取到有效的检查结果。");
		//	////////////////////////////////////////////////
		//}

		if (tokenCheckResultInfo == null || tokenCheckResultInfo.CheckResult == TokenCheckResult.ValidToken)
		{
			// !!!
			httpContext.Items[Constants.HttpContextItemKeys.BrServiceTokenManager.TokenCheckResultInfo]
			    = tokenCheckResultInfo;
			// !!!
			return null;
		}

		//httpContext.Response.StatusCode = (int)tokenCheckResultInfo.ResponseStatusCode;
		var checkFailedResponse = tokenCheckResultInfo.ResponseResult ?? new Response()
		{
			Error = Error.UnknowError,
			ErrorDescription = "未知的错误。"
		};
		return checkFailedResponse;
	}

	#endregion


	////////////////////////////////////////////////
	// @重载
	////////////////////////////////////////////////

	#region 重载

	public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		if (!_isTokenCheckerServiceGot)
		{
			_tokenCheckerService ??= BaoXia.Utils.Environment.GetService<IUserTokenCheckerService>();
			_isTokenCheckerServiceGot = true;
		}
		if (_tokenCheckerService == null)
		{
			// !!!⚠
			await next();
			// !!!⚠
			return;
		}

		var tokenCheckResultInfo = await _tokenCheckerService.CheckTokenInHttpContextAsync(context.HttpContext, context, null, null);
		//if (tokenCheckResultInfo == null)
		//{
		//	////////////////////////////////////////////////
		//	throw new ArgumentNullException(
		//		nameof(tokenCheckResultInfo),
		//		"无法从令牌检查服务中，获取到有效的检查结果。");
		//	////////////////////////////////////////////////
		//}

		if (tokenCheckResultInfo == null || tokenCheckResultInfo.CheckResult == TokenCheckResult.ValidToken)
		{
			// !!!
			context.HttpContext.Items[Constants.HttpContextItemKeys.BrServiceTokenManager.TokenCheckResultInfo]
			    = tokenCheckResultInfo;
			// !!!

			// !!!⚠
			await next();
			// !!!⚠
			return;
		}

		////////////////////////////////////////////////
		// !!!
		context.HttpContext.Response.StatusCode = (int)tokenCheckResultInfo.ResponseStatusCode;
		var responseResult = tokenCheckResultInfo.ResponseResult;
		responseResult ??= new Response()
		{
			Error = Error.UnknowError
		};
		// !!!
		context.Result = new JsonResult(responseResult);
		// !!!
		////////////////////////////////////////////////
	}


	#endregion
}
