using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Interfaces;
using Boar.Red.Service.Attributes;
using Boar.Red.Service.BrService.Interfaces;
using Boar.Red.Service.BrService.ViewModels;
using Boar.Red.Service.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace Boar.Red.Service.Filters;

public class RequestValidCheckerFilter(ILogFile? exceptionLogFile, string[]? httpHeaderKeysForValueNecessary) : IActionFilter
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ILogFile? ExceptionLogFile { get; set; } = exceptionLogFile;

	public string[]? HttpHeaderKeysForValueNecessary { get; set; } = httpHeaderKeysForValueNecessary;

	#endregion



	////////////////////////////////////////////////
	// @静态缓存
	////////////////////////////////////////////////

	#region 静态缓存

	private static readonly ConcurrentDictionary<Type, object[]>
		_httpHeaderKeysNecessaryOptionAttributesInWebSocketServiceTypes = new();

	/// <summary>
	/// 获取指定“WebSocket服务类型”上的“Http头部必要参数检查”特性集合。
	/// 带有缓存，避免“鉴权”每执行一次，就执行一次反射。
	/// </summary>
	private static object[] GetHttpHeaderKeysNecessaryOptionAttributesInWebSocketServiceType(
		Type webSocketServiceType)
	{
		return _httpHeaderKeysNecessaryOptionAttributesInWebSocketServiceTypes.GetOrAdd(
			webSocketServiceType,
			(type) => type.GetCustomAttributes(
				typeof(HttpHeaderKeysForValueNecessaryOptionAttribute), false));
	}

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public Response? CheckRequestHeadersForWebSocketServiceInHttpContext(
		HttpContext httpContext, IWebSocketService webSocketService, WebSocketServiceRequest? webSocketServiceRequest)
	{
		var httpHeaderKeysNecessaryOptionAttributes = GetHttpHeaderKeysNecessaryOptionAttributesInWebSocketServiceType(
			webSocketService.GetType());
		var isHttpHeaderKeysNecessaryNeedCheck = true;
		string[]? httpHeaderKeysNeedCheck = null;
		string[]? httpHeaderKeysNotNeedCheck = null;
		foreach (var httpHeaderKeysNecessaryOptionAttributeObject in httpHeaderKeysNecessaryOptionAttributes)
		{
			if (httpHeaderKeysNecessaryOptionAttributeObject
			    is not HttpHeaderKeysForValueNecessaryOptionAttribute httpHeaderKeysNecessaryOptionAttribute)
			{
				continue;
			}
			//
			isHttpHeaderKeysNecessaryNeedCheck
			    = httpHeaderKeysNecessaryOptionAttribute.HttpHeaderKeysCheckType
			    == HttpHeaderKeysForValueNecessaryCheckType.NeedCheck;
			httpHeaderKeysNeedCheck
			    = httpHeaderKeysNecessaryOptionAttribute.HttpHeaderKeysNeedCheck;
			httpHeaderKeysNotNeedCheck
			    = httpHeaderKeysNecessaryOptionAttribute.HttpHeaderKeysNotNeedCheck;
			//
			break;
		}
		var httpHeaderKeysForValueNecessary = this.HttpHeaderKeysForValueNecessary;
		if (httpHeaderKeysNeedCheck?.Length > 0)
		{
			// !!!
			httpHeaderKeysForValueNecessary = httpHeaderKeysNeedCheck;
			// !!!
		}
		if (!isHttpHeaderKeysNecessaryNeedCheck
			|| httpHeaderKeysForValueNecessary == null || httpHeaderKeysForValueNecessary.Length < 1)
		{
			return null;
		}


		StringBuilder? invalidHttpHeaderValueKeysBuilder = null;
		foreach (var httpHeaderKey in httpHeaderKeysForValueNecessary)
		{
			if (string.IsNullOrEmpty(httpHeaderKey))
			{
				continue;
			}
			if (httpHeaderKeysNotNeedCheck?.Length > 0)
			{
				var isHttpHeaderKeyNotNeedCheck = false;
				foreach (var httpHeaderKeyNotNeedCheck in httpHeaderKeysNotNeedCheck)
				{
					if (httpHeaderKey.EqualsIgnoreCase(httpHeaderKeyNotNeedCheck))
					{
						//
						isHttpHeaderKeyNotNeedCheck = true;
						break;
						//
					}
				}
				if (isHttpHeaderKeyNotNeedCheck)
				{
					continue;
				}
			}

			if (!httpContext.Request.Headers.TryGetValue(httpHeaderKey, out _))
			{
				if (webSocketService != null)
				{
					if (webSocketServiceRequest?.CustomHttpHeaders?.TryGetValue(httpHeaderKey, out _) == true)
					{
						continue;
					}
				}

				// !!!
				invalidHttpHeaderValueKeysBuilder ??= new();
				if (invalidHttpHeaderValueKeysBuilder.Length > 0)
				{
					invalidHttpHeaderValueKeysBuilder.Append('，');
				}
				invalidHttpHeaderValueKeysBuilder.Append(httpHeaderKey);
				// !!!
			}
		}


		if (invalidHttpHeaderValueKeysBuilder != null)
		{
			return new Response()
			{
				Error = Error.InvalidRequest,
				ErrorDescription = "缺少必要的Http头部参数：" + invalidHttpHeaderValueKeysBuilder.ToString() + "。"
			};
		}
		return null;
	}

	#endregion


	////////////////////////////////////////////////
	// @重载
	////////////////////////////////////////////////

	#region 重载

	public void OnActionExecuting(ActionExecutingContext context)
	{
		////////////////////////////////////////////////
		// 1/2, 检查必要的Http头。
		////////////////////////////////////////////////
		if (HttpHeaderKeysForValueNecessary is string[] httpHeaderKeysForValueNecessary
		    && httpHeaderKeysForValueNecessary.Length > 0)
		{
			var httpHeaderKeysNecessaryOptionAttributes = Array.Empty<object>();
			if (context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
			{
				httpHeaderKeysNecessaryOptionAttributes
				    = controllerActionDescriptor.MethodInfo.GetCustomAttributes(typeof(HttpHeaderKeysForValueNecessaryOptionAttribute),
				    false);
			}
			var controllerType = context.Controller.GetType();
			if (httpHeaderKeysNecessaryOptionAttributes.Length < 1)
			{
				httpHeaderKeysNecessaryOptionAttributes = controllerType.GetCustomAttributes(
				    typeof(HttpHeaderKeysForValueNecessaryOptionAttribute),
				    false);
			}


			var isHttpHeaderKeysNecessaryNeedCheck = true;
			string[]? httpHeaderKeysNeedCheck = null;
			string[]? httpHeaderKeysNotNeedCheck = null;
			foreach (var httpHeaderKeysNecessaryOptionAttributeObject in httpHeaderKeysNecessaryOptionAttributes)
			{
				if (httpHeaderKeysNecessaryOptionAttributeObject
				    is not HttpHeaderKeysForValueNecessaryOptionAttribute httpHeaderKeysNecessaryOptionAttribute)
				{
					continue;
				}
				//
				isHttpHeaderKeysNecessaryNeedCheck
				    = httpHeaderKeysNecessaryOptionAttribute.HttpHeaderKeysCheckType
				    == HttpHeaderKeysForValueNecessaryCheckType.NeedCheck;
				httpHeaderKeysNeedCheck
				    = httpHeaderKeysNecessaryOptionAttribute.HttpHeaderKeysNeedCheck;
				httpHeaderKeysNotNeedCheck
				    = httpHeaderKeysNecessaryOptionAttribute.HttpHeaderKeysNotNeedCheck;
				//
				break;
			}
			if (httpHeaderKeysNeedCheck?.Length > 0)
			{
				// !!!
				httpHeaderKeysForValueNecessary = httpHeaderKeysNeedCheck;
				// !!!
			}


			StringBuilder? invalidHttpHeaderValueKeysBuilder = null;
			if (isHttpHeaderKeysNecessaryNeedCheck)
			{
				foreach (var httpHeaderKey in httpHeaderKeysForValueNecessary)
				{
					if (string.IsNullOrEmpty(httpHeaderKey))
					{
						continue;
					}
					if (httpHeaderKeysNotNeedCheck?.Length > 0)
					{
						var isHttpHeaderKeyNotNeedCheck = false;
						foreach (var httpHeaderKeyNotNeedCheck in httpHeaderKeysNotNeedCheck)
						{
							if (httpHeaderKey.EqualsIgnoreCase(httpHeaderKeyNotNeedCheck))
							{
								//
								isHttpHeaderKeyNotNeedCheck = true;
								break;
								//
							}
						}
						if (isHttpHeaderKeyNotNeedCheck)
						{
							continue;
						}
					}

					if (!context.HttpContext.Request.Headers.TryGetValue(httpHeaderKey, out _))
					{
						// !!!
						invalidHttpHeaderValueKeysBuilder ??= new();
						if (invalidHttpHeaderValueKeysBuilder.Length > 0)
						{
							invalidHttpHeaderValueKeysBuilder.Append('，');
						}
						invalidHttpHeaderValueKeysBuilder.Append(httpHeaderKey);
						// !!!
					}
				}
			}
			if (invalidHttpHeaderValueKeysBuilder != null)
			{
				// !!!
				context.Result = new JsonResult(new Response()
				{
					Error = Error.InvalidRequest,
					ErrorDescription = "缺少必要的Http头部参数：" + invalidHttpHeaderValueKeysBuilder.ToString() + "。"
				});
				return;
				// !!!
			}
		}


		////////////////////////////////////////////////
		// 2/2, 检查请求参数是否有效。
		////////////////////////////////////////////////
		var actionArgumentsNeed = (context.ActionDescriptor as ControllerActionDescriptor)?.Parameters;
		if (actionArgumentsNeed == null
		    || actionArgumentsNeed.Count < 1)
		{
			return;
		}

		var actionArguments = context.ActionArguments;
		//if (actionArguments == null
		//	|| actionArguments.Count < actionArgumentsNeed.Count)
		//{
		//	////////////////////////////////////////////////
		//	// !!!
		//	context.Result = new JsonResult(new Response()
		//	{
		//		Error = Error.InvalidRequest,
		//		ErrorDescription = "请求参数数量不足。"
		//	});
		//	////////////////////////////////////////////////
		//	return;
		//}
		var nullabilityInfoContext = new NullabilityInfoContext();
		foreach (var actionArgumentNeed in actionArgumentsNeed)
		{
			if (actionArguments.TryGetValue(
			    actionArgumentNeed.Name,
			    out var actionArgumentValue) == true)
			{
				if (actionArgumentValue is BaoXia.Constants.Models.Request bxRequest)
				{
					if (bxRequest.GetErrorResonseByCheckRequest(RequestPipelineType.Http) is Response errorResponse)
					{
						////////////////////////////////////////////////
						// !!!
						//context.Result = new JsonResult(new Response()
						//{
						//	Error = Error.InvalidRequest,
						//	ErrorDescription = $"请求参数（{actionArgumentNeed.Name}）无效。",
						//	ChildResult = errorResponse
						//});
						context.Result = new JsonResult(errorResponse);
						// !!!
						break;
						// !!!
						////////////////////////////////////////////////
					}
				}
			}
			else
			{
				NullabilityInfo? actionArgumentNeedNullableInfo = null;
				if (actionArgumentNeed is ControllerParameterDescriptor actionArgumentNeedDescriptor)
				{
					actionArgumentNeedNullableInfo = nullabilityInfoContext.Create(actionArgumentNeedDescriptor.ParameterInfo);
				}
				if (actionArgumentNeedNullableInfo == null
				    || actionArgumentNeedNullableInfo.WriteState != NullabilityState.Nullable)
				{

					////////////////////////////////////////////////
					// !!!
					context.Result = new JsonResult(new Response()
					{
						Error = Error.InvalidRequest,
						ErrorDescription = $"请求参数（{actionArgumentNeed.Name}）不存在。"
					});
					// !!!
					break;
					// !!!
					////////////////////////////////////////////////
				}
			}
		}
	}

	public void OnActionExecuted(ActionExecutedContext context)
	{
		if (context.Exception == null)
		{
			return;
		}

		ApiNameAttribute? apiNameAttribute = null;
		if (context.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
		{
			apiNameAttribute
			    = actionDescriptor
			    .MethodInfo
			    .GetCustomAttributes(typeof(ApiNameAttribute), true)
			    .FirstOrDefault()
			    as ApiNameAttribute;
		}
		var apiName = apiNameAttribute?.Name ?? context.HttpContext.Request.Path;
		var apiLogContentForException = apiNameAttribute?.LogContentForException ?? apiName + "失败，程序异常。";

		// !!! 记录异常日志 !!!
		{
			ExceptionLogFile?.Logs(this, apiLogContentForException, context.Exception);
		}
		////////////////////////////////////////////////
		// !!!
		context.Exception = null;
		// !!!
		////////////////////////////////////////////////
		context.Result = new JsonResult(new Response()
		{
			Error = Error.ProgramError
		});
	}

	#endregion
}
