using BaoXia.Utils;
using BaoXia.Utils.Interfaces;
using Boar.Red.Service.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Boar.Red.Service.Extensions;

public static class ServiceAdministratorServiceExtension
{
	////////////////////////////////////////////////
	// “WebApplicationBuilder”的“BrService”扩展方法。
	////////////////////////////////////////////////

	#region “WebApplicationBuilder”的“BrService”扩展方法。

	public static WebApplicationBuilder ConfigureServiceAdministratorService(
	    this WebApplicationBuilder applicationBuilder,
	    ConfigFile configFile,
	    string configPropertyNameInConfigFile,
	    //
	    ILogFile? logFile,
	    ILogFile? exceptionLogFile,
	    ILogFile? warningLogFile)
	{
		////////////////////////////////////////////////
		// 初始化单例的服务中间件：
		////////////////////////////////////////////////
		var serviceAdministratorSessionService
		    = new ServiceAdministratorService(
		    configFile,
		    configPropertyNameInConfigFile,
		    //
		    logFile,
		    exceptionLogFile,
		    warningLogFile);
		applicationBuilder
		    .Services
		    .AddSingleton<ServiceAdministratorService>(
		    (serviceMiddlewareProvider) =>
		    {
			    return serviceAdministratorSessionService;
		    });
		return applicationBuilder;
	}

	#endregion
}