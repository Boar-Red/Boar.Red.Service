using BaoXia.Utils.Interfaces;
using Boar.Red.Service.WebService.Middlewares;
using Boar.Red.Service.WebService.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Boar.Red.Service.WebService.Extensions;

public static class HttpRequestCrossDomainControlMiddlewareExtension
{
	////////////////////////////////////////////////
	// “WebApplicationBuilder”的“HttpRequestCrossDomainControl”扩展方法。
	////////////////////////////////////////////////

	#region 类方法

	public static WebApplicationBuilder ConfigureCrossDomainControl(
	    this WebApplicationBuilder webApplicationBuilder,
	    Func<CrossDomainControlInfo?> toGetCrossDomainControlInfo,
	    //
	    ILogFile? logFile,
	    ILogFile? exceptionLogFile,
	    ILogFile? warningLogFile)
	{
		////////////////////////////////////////////////
		// 配置服务中间件：
		////////////////////////////////////////////////
		webApplicationBuilder.Services
		    .AddSingleton<HttpRequestCrossDomainControlMiddleware>(
		    (serviceMiddlewareProvider) =>
		    {
			    return new HttpRequestCrossDomainControlMiddleware(
				    toGetCrossDomainControlInfo, logFile, exceptionLogFile, exceptionLogFile);
		    });

		return webApplicationBuilder;
	}

	#endregion


	////////////////////////////////////////////////
	// “IApplicationBuilder”的“HttpRequestCrossDomainControl”扩展方法。
	////////////////////////////////////////////////

	#region “IApplicationBuilder”的“HttpRequestCrossDomainControl”扩展方法。

	public static IApplicationBuilder UseCrossDomainControl(
	    this IApplicationBuilder builder)
	{
		return builder.UseMiddleware<HttpRequestCrossDomainControlMiddleware>();
	}

	#endregion
}
