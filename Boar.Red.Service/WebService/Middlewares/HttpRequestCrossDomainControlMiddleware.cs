using BaoXia.Utils;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Interfaces;
using Boar.Red.Service.WebService.Models;
using Microsoft.AspNetCore.Http;

namespace Boar.Red.Service.WebService.Middlewares;

public class HttpRequestCrossDomainControlMiddleware(
	Func<CrossDomainControlInfo?> toGetCrossDomainControlInfo, ILogFile? logFile, ILogFile? exceptionLogFile, ILogFile? warningLogFile)
	: MiddlewareAsync(logFile, exceptionLogFile, warningLogFile)
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性			

	public Func<CrossDomainControlInfo?> ToGetCrossDomainControlInfo { get; set; } = toGetCrossDomainControlInfo;

	#endregion


	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static bool IsDomainInOrSubdomainOfDomainList(
	    string? domain,
	    IEnumerable<string>? credibleDomains)
	{
		if (string.IsNullOrEmpty(domain))
		{
			return false;
		}
		if (credibleDomains == null
		    || credibleDomains.IsNotEmpty() == false)
		{
			return false;
		}

		var indexOfDomainHostBegin = 0;
		var urlSchemeDelimeter = "://";
		var indexOfUrlSchemeDelimeter = domain.IndexOf(urlSchemeDelimeter);
		if (indexOfUrlSchemeDelimeter >= 0)
		{
			indexOfDomainHostBegin
			    = indexOfUrlSchemeDelimeter
			    + urlSchemeDelimeter.Length;
		}
		var indexOfDomainHostEnd = indexOfDomainHostBegin;
		var domainLength = domain.Length;
		for (;
		      indexOfDomainHostEnd < domainLength;
		      indexOfDomainHostEnd++)
		{
			var domainCharacter = domain[indexOfDomainHostEnd];
			if (domainCharacter == ':'
			    || domainCharacter == '/'
			    || domainCharacter == '?'
			    || domainCharacter == '#')
			{
				break;
			}
		}
		if (indexOfDomainHostEnd <= indexOfDomainHostBegin)
		{
			return false;
		}

		foreach (var credibleDomain
		    in
		    credibleDomains)
		{
			var indexOfCredibleDomainBegin
			    = domain.IndexOfIgnoreCase(
			    credibleDomain,
			    indexOfDomainHostBegin);
			var isValidCredibleDomainBegin = false;
			// 当前请求的域名，以可信任的域名开始：
			if (indexOfCredibleDomainBegin == indexOfDomainHostBegin)
			{
				isValidCredibleDomainBegin = true;
			}
			// 当前请求的域名，是可信任的域名的子域名：
			else if (indexOfCredibleDomainBegin > indexOfDomainHostBegin
			    && domain[indexOfCredibleDomainBegin - 1] == '.')
			{
				isValidCredibleDomainBegin = true;
			}
			if (!isValidCredibleDomainBegin)
			{
				continue;
			}

			var indexOfCredibleDomainEnd
			    = indexOfCredibleDomainBegin
			    + credibleDomain.Length;
			// 当前请求的域名，以可信任的域名结束：
			var isValidCredibleDomainEnd = false;
			if (indexOfCredibleDomainEnd == indexOfDomainHostEnd)
			{
				isValidCredibleDomainEnd = true;
			}
			if (isValidCredibleDomainEnd == false)
			{
				continue;
			}

			////////////////////////////////////////////////
			// !!!
			return true;
			// !!!
			////////////////////////////////////////////////
		}
		return false;
	}

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现		

	public bool IsAccessControllAllowOrigin(string? domain)
	{
		if (domain == null
		    || domain.Length < 1)
		{
			// !!!⚠ 没有明确的来源信息时，默认允许请求 ⚠!!!
			return true;
		}
		var controlInfo = this.ToGetCrossDomainControlInfo();
		if (controlInfo == null)
		{
			// !!!⚠ 没有明确的控制信息时，默认允许请求 ⚠!!!
			return true;
		}

		if (controlInfo.IsAnyDomainAccessControllAllow == true)
		{
			return true;
		}

		var credibleDomains = controlInfo.Access_Control_Allow_Origins;
		{ }
		return HttpRequestCrossDomainControlMiddleware
		    .IsDomainInOrSubdomainOfDomainList(
		    domain,
		    credibleDomains);
	}

	#endregion

	////////////////////////////////////////////////
	// @实现“Middleware”定义方法
	////////////////////////////////////////////////

	#region 实现“Middleware”

	protected override async Task<bool> DidIfGoToNextMiddlewareByInvokeAsync(
	    HttpContext httpContext)
	{
		////////////////////////////////////////////////
		// 1/2，跨域控制：
		////////////////////////////////////////////////
		try
		{
			var request = httpContext.Request;
			{
				var requestHeaders = request.Headers;
				var response = httpContext.Response;
				var responseHeaders = response.Headers;
				string? requestOrigin = null;
				var requestOriginHeaderValues = requestHeaders.Origin;
				if (requestOriginHeaderValues.Count > 0)
				{
					requestOrigin = requestOriginHeaderValues[0];
				}

				var isValidOrigin = this.IsAccessControllAllowOrigin(requestOrigin);
				if (isValidOrigin)
				{
					response.StatusCode = (int)System.Net.HttpStatusCode.OK;
					response.Headers.AddOrSet(
					    "Access-Control-Allow-Origin",
					    requestOrigin);
				}
				else
				{
					response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
				}

				var controlInfo = this.ToGetCrossDomainControlInfo();
				if (controlInfo != null)
				{
					responseHeaders.AddOrSet(
					    "Access-Control-Allow-Methods",
					    controlInfo.Access_Control_Allow_Methods);

					responseHeaders.AddOrSet(
					    "Access-Control-Allow-Headers",
					    controlInfo.Access_Control_Allow_Headers);

					responseHeaders.AddOrSet(
					    "Access-Control-Allow-Credentials",
					    controlInfo.Access_Control_Allow_Credentials
					    ? "true"
					    : "false");
				}

				if (isValidOrigin == false
				    || System.Net.Http.HttpMethod
				    .Options
				    .Method
				    .EqualsIgnoreCase(request.Method))
				{
					// !!!
					await response.CompleteAsync();
					// !!!
					return false;
				}
			}
		}
		catch (Exception exception)
		{
			//
			ExceptionLogFile?.Logs(this, "跨域访问控制失败，程序异常。", exception);
			//
			return false;
		}


		////////////////////////////////////////////////
		// 2/2，继续响应通道：
		////////////////////////////////////////////////
		// !!!
		return true;
		// !!!
	}

	#endregion
}