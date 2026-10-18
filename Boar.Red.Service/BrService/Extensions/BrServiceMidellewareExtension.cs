using BaoXia.Utils;
using BaoXia.Utils.Interfaces;
using Boar.Red.Service.BrService.Middlewares;
using Boar.Red.Service.BrService.Models.StatisticsInfo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Boar.Red.Service.BrService.Extensions;

public static class BrServiceMidellewareExtension
{
	////////////////////////////////////////////////
	// “WebApplicationBuilder”的“BrService”扩展方法。
	////////////////////////////////////////////////

	#region “WebApplicationBuilder”的“BrService”扩展方法。

	public static WebApplicationBuilder ConfigureBrService(
	    this WebApplicationBuilder applicationBuilder,
	    bool isControllerBrServiceDefault,
	    Dictionary<string, string>? serviceRouteTempaltes,
	    //
	    //
	    ConfigFile? webServiceConfigFile,
	    string? webServiceConfigPropertyNameInConfigFile,
	    ConfigFile? bxServiceConfigFile,
	    string? bxServiceConfigPropertyNameInConfigFile,
	    //
	    //
	    ILogFile? logFile,
	    ILogFile? exceptionLogFile,
	    ILogFile? warningLogFile,
	    ILogFile? bxServiceLogFile,
	    //
	    ServiceDiscoverer? serviceDiscoverSpecified,
	    //
	    Action<Action<string, double>, CancellationToken>? toServiceBooting,
	    Action<CancellationToken>? toServiceStarted,
	    Action<CancellationToken>? toServiceShutdown,
	    Func<string?, bool>? toIsClientVersionSupported,
	    //
	    Func<double>? toGetServiceLoadRate,
	    Action<ServiceRequestStatisticsInfo[]>? toSaveServiceRequestStatisticsInfes)
	{
		////////////////////////////////////////////////
		// 初始化单例的服务中间件：
		////////////////////////////////////////////////

		////////////////////////////////////////////////
		// 1/2，服务中间件包括监听UDP等需要自启动的功能，
		// 因此需要进行默认初始化，而不是被动初始化：
		////////////////////////////////////////////////
		var bxServiceMiddleware = new BrServiceMiddleware(
			isControllerBrServiceDefault,
			serviceRouteTempaltes,
			//
			webServiceConfigFile,
			webServiceConfigPropertyNameInConfigFile,
			//
			bxServiceConfigFile,
			bxServiceConfigPropertyNameInConfigFile,
			//
			//
			logFile,
			exceptionLogFile,
			warningLogFile,
			bxServiceLogFile,
			//
			serviceDiscoverSpecified,
			//
			toServiceBooting,
			toServiceStarted,
			toServiceShutdown,
			toIsClientVersionSupported,
			//
			toGetServiceLoadRate,
			toSaveServiceRequestStatisticsInfes);
		////////////////////////////////////////////////
		// 2/2，配置服务中间件：
		////////////////////////////////////////////////
		applicationBuilder.Services.AddSingleton<BrServiceMiddleware>(
		    (serviceMiddlewareProvider) =>
		    {
			    return bxServiceMiddleware;
		    });
		return applicationBuilder;
	}

	#endregion


	////////////////////////////////////////////////
	// “IApplicationBuilder”的“BrService”扩展方法。
	////////////////////////////////////////////////

	#region “IApplicationBuilder”的“BrService”扩展方法。

	public static IApplicationBuilder UseBrService(this IApplicationBuilder application, bool isRegisterApplicationNormalExitEvent = true)
	{
		////////////////////////////////////////////////
		// 添加中间件：
		////////////////////////////////////////////////
		application.UseMiddleware<BrServiceMiddleware>();


		////////////////////////////////////////////////
		// 注册生命周期事件：
		////////////////////////////////////////////////
		if (isRegisterApplicationNormalExitEvent && application is WebApplication webApplication)
		{
			webApplication.Lifetime.ApplicationStopped.Register(() =>
			{
				var bxServiceMiddleware = webApplication.Services.GetRequiredService<BrServiceMiddleware>();
				// !!!
				bxServiceMiddleware?.SetApplicationNormalExit();
				// !!!
			});
		}
		return application;
	}

	public static IEndpointRouteBuilder UseControllersWithViewsService(
		this IEndpointRouteBuilder application, Dictionary<string, string> routeTemplates)
	{
		foreach (var routeTemplate in routeTemplates)
		{
			application.MapControllerRoute(name: routeTemplate.Key, pattern: routeTemplate.Value);
		}
		return application;
	}

	#endregion
}