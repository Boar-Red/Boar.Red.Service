using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Boar.Red.Service.BrService.Extensions;

public static class CsrfTokenManagerExtension
{
	////////////////////////////////////////////////
	// “WebApplicationBuilder”的“BrService”扩展方法。
	////////////////////////////////////////////////

	#region “WebApplicationBuilder”的“BrService”扩展方法。

	public static WebApplicationBuilder ConfigureCsrfTokenManager(
	    this WebApplicationBuilder applicationBuilder,
	    Func<double> toGetTempTokenLiveSecondsMax,
	    Func<int>? toGetTokenValueLength,
	    Func<double>? toGetTokenCleanIntervalSeconds = null)
	{
		////////////////////////////////////////////////
		// 初始化单例的服务：
		////////////////////////////////////////////////

		applicationBuilder
		    .Services
		    .AddSingleton<CsrfTokenManager>(
		    (serviceMiddlewareProvider) =>
		    {
			    return new CsrfTokenManager(
		toGetTempTokenLiveSecondsMax,
		toGetTokenValueLength,
		toGetTokenCleanIntervalSeconds);
		    });
		return applicationBuilder;
	}

	public static WebApplicationBuilder ConfigureCsrfTokenManager(
	    this WebApplicationBuilder applicationBuilder,
	    Func<CsrfTokenManagerConfig?> toGetCsrfTokenManagerConfig)
	{
		////////////////////////////////////////////////
		// 初始化单例的服务：
		////////////////////////////////////////////////

		applicationBuilder
		    .Services
		    .AddSingleton<CsrfTokenManager>(
		    (serviceMiddlewareProvider) =>
		    {
			    return new CsrfTokenManager(
		() => toGetCsrfTokenManagerConfig()?.CsrfTokenLiveSecondsMax ?? 0,
		() => toGetCsrfTokenManagerConfig()?.CsrfTokenLength ?? 0,
		() => toGetCsrfTokenManagerConfig()?.TokenCleanIntervalSeconds ?? 0);
		    });
		return applicationBuilder;
	}

	#endregion
}