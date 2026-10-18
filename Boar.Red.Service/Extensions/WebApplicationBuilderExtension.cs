using BaoXia.Utils;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Interfaces;
using Boar.Red.Service.Attributes;
using Boar.Red.Service.BrService;
using Boar.Red.Service.BrService.Extensions;
using Boar.Red.Service.BrService.Interfaces;
using Boar.Red.Service.BrService.Middlewares;
using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.BrService.ViewModels;
using Boar.Red.Service.Constants;
using Boar.Red.Service.WebService.Extensions;
using Boar.Red.Service.WebService.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;


namespace Boar.Red.Service.Extensions;

public static class WebApplicationBuilderExtension
{

	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	/// <summary>
	/// 配置，并开启Web服务。
	/// </summary>
	/// <param name="appBuilder">当前应用程序创建。</param>
	/// <param name="serverName">当前服务器名称。</param>
	/// <param name="currentProjectAssembly">明确指出当前项目的程序集，避免在程序包管理器控制台，执行EF命令等操作时，无法扫描到当前项目中的服务。</param>
	/// <param name="httpServer">当前使用的Http服务器。</param>
	/// <param name="enviromentName">当前运行环境名称，通常使用“BaoXia.Utils.Environment.GetEnvironmentNameWith_ASPNETCORE_ENVIRONMENT()”获取。</param>
	/// <param name="aesKeyDeafult">全局默认的AES加密私钥。</param>
	/// <param name="configFilesDirectoryPath">配置文件的路径（相对，或绝对）。</param>
	/// <param name="logFilesDirectoryPath">日志文件的路径（相对，或绝对）。</param>
	/// <param name="logFile">普通日志文件对象。</param>
	/// <param name="exceptionLogFile">异常日志文件对象。</param>
	/// <param name="warningLogFile">警告日志文件对象。</param>
	/// <param name="webServiceConfigFile">Web服务配置所在的配置文件对象。</param>
	/// <param name="webServiceConfigPropertyNameInConfigFile">Web服务配置所在的配置文件对象中的属性名称。</param>
	/// <param name="controllerRouteTemplates">控制器路由模板。</param>
	/// <param name="bxServiceInitializationParam">Boar.Red 服务初始化参数。</param>
	/// <param name="serviceAdministratorInitializationParam">服务管理员服务初始化参数。</param>
	/// <param name="toConfigureService">配置业务服务到容器。</param>
	/// <param name="toConfigureRequestPipeBegin">配置程序请求管道的，开始处。</param>
	/// <param name="toConfigureRequestPipeTrailer">配置程序请求管道的，尾部。</param>
	/// <param name="middleware">可自定义的中间件。</param>
	/// <param name="environmentInitializeParam">环境初始化参数，如：日志记录参数等，通常使用默认值。</param>
	public static void RunWithServerName(
	    this WebApplicationBuilder appBuilder,
	    ////////////////////////////////////////////////
	    string? serverName,
	    string? enviromentName,
	    Assembly? currentProjectAssembly,
	    HttpServer httpServer,
	    //
	    string aesKeyDeafult,
	    //
	    string configFilesDirectoryPath,
	    string logFilesDirectoryPath,
	    ILogFile? logFile,
	    ILogFile? exceptionLogFile,
	    ILogFile? warningLogFile,
	    ILogFile? bxServiceLogFile,
	    //
	    ConfigFile? webServiceConfigFile,
	    string? webServiceConfigPropertyNameInConfigFile,
	    //
	    Dictionary<string, string>? controllerRouteTemplates = null,
	    ////////////////////////////////////////////////
	    ServiceInitializationParam? bxServiceInitializationParam = null,
	    ////////////////////////////////////////////////
	    ServiceAdministratorInitializationParam? serviceAdministratorInitializationParam = null,
	    ////////////////////////////////////////////////
	    Func<WebApplicationBuilder, WebApplicationBuilder>? toConfigureService = null,
	    ////////////////////////////////////////////////
	    Func<WebApplication, WebApplication>? toConfigureRequestPipeBegin = null,
	    Func<WebApplication, WebApplication>? toConfigureRequestPipeTrailer = null,
	    ////////////////////////////////////////////////
	    Func<HttpContext, Func<Task>, Task>? toUseMiddleware = null,
	    ////////////////////////////////////////////////
	    //
	    EnvironmentInitializeParam? environmentInitializeParam = null,
	    //
	    ////////////////////////////////////////////////
	    Func<WebApplication, WebApplication>? toConfigureStaticFileService = null)
	{
		try
		{
			////////////////////////////////////////////////
			// 1/8，配置默认参数：
			////////////////////////////////////////////////
			controllerRouteTemplates ??= RouteTemplateConstants.RouteTemplatesDefault;
			var isBrServiceEnable = bxServiceInitializationParam?.IsBrServiceEnable == true;
			var isWebSocketEnable = bxServiceInitializationParam?.IsWebSocketEnable == true || isBrServiceEnable;

			////////////////////////////////////////////////
			// 2/8，配置环境信息（1/2）：
			////////////////////////////////////////////////
			BaoXia.Utils.Environment.InitializeBeforeConfigureServicesWithServerName(
			    serverName,
			    enviromentName,
			    currentProjectAssembly,
			    //
			    aesKeyDeafult,
			    //
			    configFilesDirectoryPath,
			    logFilesDirectoryPath,
			    //

			    //
			    environmentInitializeParam);

			////////////////////////////////////////////////
			// 3/8，展示服务启动信息：
			////////////////////////////////////////////////
			var webServiceConfigInfo = new ConfigInfoInConfigFile<WebServiceConfig>(
			    webServiceConfigFile,
			    webServiceConfigPropertyNameInConfigFile,
			    WebServiceConfig.Default,
			    null);
			var webServiceConfig
			    = webServiceConfigInfo.Config;
			{
				ServiceConfig? bxServiceConfig = null;
				if (bxServiceInitializationParam?.BrServiceConfigFile
				       is ConfigFile bxServiceConfigFile
				       && bxServiceInitializationParam?.BrServiceConfigPropertyNameInConfigFile
				       is string bxServiceConfigPropertyNameInConfigFile)
				{
					var bxServiceConfigInfo = new ConfigInfoInConfigFile<ServiceConfig>(
					    bxServiceConfigFile,
					    bxServiceConfigPropertyNameInConfigFile,
					    ServiceConfig.Default,
					    null);
					//
					bxServiceConfig = bxServiceConfigInfo.Config;
					//
				}
				Utils.ServiceConsoleUtil.WriteServiceInfoWithWebServiceConfig(
				    webServiceConfig,
				    bxServiceConfig,
				    bxServiceInitializationParam?.ServiceDiscoverSpecified);
			}

			////////////////////////////////////////////////
			// 4/8，配置“应用程序创建器”，配置支持的服务：
			////////////////////////////////////////////////
			{
				////////////////////////////////////////////////
				// 1/5，配置Web服务：
				////////////////////////////////////////////////
				appBuilder.ConfigureWebService(
				    httpServer,
				    webServiceConfig,
				    true,
				    //
				    BaoXia.Utils.Environment.JsonSerializerOptions,
				    //
				    logFile,
				    exceptionLogFile,
				    warningLogFile);


				////////////////////////////////////////////////
				// 2/5，配置长连接服务：
				////////////////////////////////////////////////
				if (bxServiceInitializationParam?.BrServiceConfigFile != null
				    && StringUtil.IsNotEmpty(
				    bxServiceInitializationParam
				    ?.BrServiceConfigPropertyNameInConfigFile))
				{
					appBuilder.ConfigureBrService(
					    bxServiceInitializationParam.IsControllerBrServiceDefault,
					    controllerRouteTemplates,
					    //
					    //
					    bxServiceInitializationParam.WebServiceConfigFile,
					    bxServiceInitializationParam.WebServiceConfigPropertyNameInConfigFile,
					    //
					    bxServiceInitializationParam.BrServiceConfigFile,
					    bxServiceInitializationParam.BrServiceConfigPropertyNameInConfigFile,
					    //
					    //
					    logFile,
					    exceptionLogFile,
					    warningLogFile,
					    bxServiceLogFile,
					    //
					    bxServiceInitializationParam.ServiceDiscoverSpecified,
					    //
					    bxServiceInitializationParam.ToServiceBooting,
					    bxServiceInitializationParam.ToServiceStarted,
					    bxServiceInitializationParam.ToServiceShutdown,
					    bxServiceInitializationParam.ToIsClientVersionSupported,
					    //
					    bxServiceInitializationParam.ToGetServiceLoadRate,
					    bxServiceInitializationParam.ToSaveServiceRequestStatisticsInfes);
				}

				////////////////////////////////////////////////
				// 3/5，配置服务管理员服务：
				////////////////////////////////////////////////
				if (serviceAdministratorInitializationParam?.ConfigFile != null
				    && StringUtil.IsNotEmpty(
				    serviceAdministratorInitializationParam
				    ?.ConfigPropertyNameInConfigFile))
				{
					appBuilder.ConfigureServiceAdministratorService(
					    serviceAdministratorInitializationParam.ConfigFile,
					    serviceAdministratorInitializationParam.ConfigPropertyNameInConfigFile,
					    //
					    logFile,
					    exceptionLogFile,
					    warningLogFile);
				}

				////////////////////////////////////////////////
				// 4/5，配置当前应用程序，
				// 启动时配置的，业务基础服务：
				////////////////////////////////////////////////
				if (toConfigureService != null)
				{
					appBuilder = toConfigureService(appBuilder);
				}

				////////////////////////////////////////////////
				// 5/5，配置当前应用程序，及程序依赖中
				// 启动后使用的，业务服务：
				////////////////////////////////////////////////
				{
					appBuilder = appBuilder.ConfigureBrServices(currentProjectAssembly);
				}
			}

			////////////////////////////////////////////////
			//  5/8，创建Application实例：
			////////////////////////////////////////////////
			var app = appBuilder.Build();

			////////////////////////////////////////////////
			// 6/8，配置环境信息（2/2）：
			////////////////////////////////////////////////
			BaoXia.Utils.Environment.InitializeAfterConfigureServices(
			    app,
			    app.Environment,
			    app.Environment.WebRootPath);

			////////////////////////////////////////////////
			// 7/8，配置应用程序请求管道：
			////////////////////////////////////////////////
			{
				////////////////////////////////////////////////
				// 配置程序请求管道的，开始处：
				////////////////////////////////////////////////
				if (toConfigureRequestPipeBegin != null)
				{
					app = toConfigureRequestPipeBegin.Invoke(app);
				}

				////////////////////////////////////////////////
				// 配置默认页面路由：
				////////////////////////////////////////////////
				if (webServiceConfig?.DefaultPageRelativePath is string defaultPageRelativePath
				    && defaultPageRelativePath.Length > 0)
				{
					_ = app.Use(async (context, next) =>
					{
						var request = context.Request;
						if (request.Path == "/" || string.IsNullOrEmpty(request.Path.Value))
						{
							request.Path = defaultPageRelativePath;
						}
						await next();
					});
				}

				////////////////////////////////////////////////
				// 开启HTTPS强制跳转：
				////////////////////////////////////////////////
				if (webServiceConfig?.IsHttpsNecessary == true)
				{
					_ = app.UseHsts();
				}

				////////////////////////////////////////////////
				// 开启使用，Web服务，跨域控制：
				////////////////////////////////////////////////
				{
					_ = app.UseCrossDomainControl();
				}

				////////////////////////////////////////////////
				// 开启使用，WebSocket服务：
				////////////////////////////////////////////////
				if (isWebSocketEnable)
				{
					_ = app.UseWebSocketService(webServiceConfig);
				}

				////////////////////////////////////////////////
				// 开启使用，长连接服务：
				////////////////////////////////////////////////
				if (isBrServiceEnable)
				{
					// !!!
					_ = app.UseBrService();
					// !!!
				}

				////////////////////////////////////////////////
				// 开启使用，静态文件服务：
				////////////////////////////////////////////////
				if (webServiceConfig?.IsStaticFilesDisable != true)
				{
					if (toConfigureStaticFileService != null)
					{
						// !!!
						app = toConfigureStaticFileService(app);
						// !!!
					}
					else
					{
						// !!!
						_ = app.UseStaticFiles();
						// !!!
					}
				}

				////////////////////////////////////////////////
				// 开启使用，HTTP请求：
				////////////////////////////////////////////////
				if (bxServiceInitializationParam?.IsHttpRequestDisable == false)
				{
					// !!!
					_ = app.UseControllersWithViewsService(
					    controllerRouteTemplates);
					// !!!
				}

				////////////////////////////////////////////////
				// 配置程序请求管道的，尾部：
				////////////////////////////////////////////////
				if (toConfigureRequestPipeTrailer != null)
				{
					app = toConfigureRequestPipeTrailer.Invoke(app);
				}

				////////////////////////////////////////////////
				// 使用中间件：
				////////////////////////////////////////////////
				_ = app.Use(async (context, next) =>
				{
					if (toUseMiddleware != null)
					{
						app.Use(toUseMiddleware);
					}
					else
					{
						await next(context);
					}
				});
			}

			////////////////////////////////////////////////
			// 8/8，开启运行：
			////////////////////////////////////////////////
			{
				// 1/2，加载&运行服务：
				if (isBrServiceEnable)
				{
					using var scope = app.Services.CreateScope();
					var bxService = scope.ServiceProvider.GetService<BrServiceMiddleware>();
					// !!!
					bxService?.Start();
					// !!!
				}

				// 2/2，运行.Net App：
				// !!!
				app.Run();
				// !!!
			}
		}
		catch (Exception exception)
		{
			if (exceptionLogFile != null)
			{
				exceptionLogFile.Logs("主程序", "程序意外退出，程序异常。", exception);
				exceptionLogFile.FlushLogBuffer();
			}
		}
	}


	/// <summary>
	/// 配置由“[Service]”特性的Boar.Red 服务对象。
	/// </summary>
	/// <param name="appBuilder">当前应用程序构建器。</param>
	/// <param name="currentProjectAssembly">明确指出当前项目的程序集，避免在程序包管理器控制台，执行EF命令等操作时，无法扫描到当前项目中的服务。</param>
	/// <returns>返回配置了Boar.Red 服务对象后的应用程序构建器。</returns>
	public static WebApplicationBuilder ConfigureBrServices(this WebApplicationBuilder appBuilder, Assembly? currentProjectAssembly)
	{
		////////////////////////////////////////////////
		// 1/，收集所有类型：
		////////////////////////////////////////////////
		List<Type> allTypes = [];
		{
			if (currentProjectAssembly != null)
			{
				var allTypesInCurrentProjectAssembly = currentProjectAssembly.GetTypes();
				if (allTypesInCurrentProjectAssembly.Length > 0)
				{
					allTypes.AddRange(allTypesInCurrentProjectAssembly);
				}
			}

			var entryAssembly = Assembly.GetEntryAssembly();
			if (entryAssembly != null && entryAssembly != currentProjectAssembly)
			{
				var allTypesInEntryAssembly = entryAssembly.GetTypes();
				if (allTypesInEntryAssembly.Length > 0)
				{
					allTypes.AddRange(allTypesInEntryAssembly);
				}
			}

			var executingAssembly = Assembly.GetExecutingAssembly();
			if (executingAssembly != null && executingAssembly != currentProjectAssembly
			    && executingAssembly != entryAssembly)
			{
				var allTypesInExecutingAssembly = executingAssembly.GetTypes();
				if (allTypesInExecutingAssembly.Length > 0)
				{
					allTypes.AddRange(allTypesInExecutingAssembly);
				}
			}

			var callingAssembly = Assembly.GetCallingAssembly();
			if (callingAssembly != null
			    && callingAssembly != currentProjectAssembly
			    && callingAssembly != entryAssembly
			    && callingAssembly != executingAssembly)
			{
				var allTypesInCallingAssembly = callingAssembly.GetTypes();
				if (allTypesInCallingAssembly.Length > 0)
				{
					allTypes.AddRange(allTypesInCallingAssembly);
				}
			}
		}
		allTypes = allTypes.RemoveIf((type) =>
		{
			if (type.IsInterface || type.IsAbstract)
			{
				return true;
			}
			return false;
		});
		allTypes = allTypes.RemoveSameItems((typeA, typeB) =>
		{
			//
			return typeA.Equals(typeB);
			//
		});


		////////////////////////////////////////////////
		// 2/，收集到类型：BrService。
		////////////////////////////////////////////////
		// !!!
		var bxServcieTypes = allTypes.ToTypesWithCustomAttributeTypeSpecified(typeof(ServiceAttribute));
		BrServiceTypesRegistered.ServiceTypes = [.. bxServcieTypes];
		// !!!
		foreach (var bxServcieType in bxServcieTypes)
		{
			var serviceAttribute = bxServcieType.GetCustomAttribute<ServiceAttribute>();
			if (serviceAttribute == null)
			{
				continue;
			}
			var serviceLifeCycle = ServiceLifeCycle.Scoped;
			if (serviceAttribute != null)
			{
				serviceLifeCycle = serviceAttribute.LifeCycle;
			}


			////////////////////////////////////////////////
			// 1/2，注册服务自身的类型实现：
			////////////////////////////////////////////////
			{
				switch (serviceLifeCycle)
				{
					case ServiceLifeCycle.Transient:
						{
							appBuilder.Services.AddTransient(bxServcieType, bxServcieType);
						}
						break;
					case ServiceLifeCycle.Scoped:
						{
							appBuilder.Services.AddScoped(bxServcieType, bxServcieType);
						}
						break;
					case ServiceLifeCycle.Singleton:
						{
							appBuilder.Services.AddSingleton(bxServcieType, bxServcieType);
						}
						break;
				}
			}


			////////////////////////////////////////////////
			// 2/2，注册服务实现接口的类型实现：
			////////////////////////////////////////////////
			var serviceImplementInterfaces = bxServcieType.GetInterfaces();
			if (serviceImplementInterfaces?.Length > 0)
			{
				switch (serviceLifeCycle)
				{
					case ServiceLifeCycle.Transient:
						{
							foreach (var serviceImplementInterface in serviceImplementInterfaces)
							{
								appBuilder.Services.AddTransient(serviceImplementInterface, (serviceProvider) =>
								{
									return serviceProvider.GetRequiredService(bxServcieType);
								});
							}
						}
						break;
					case ServiceLifeCycle.Scoped:
						{
							foreach (var serviceImplementInterface in serviceImplementInterfaces)
							{
								appBuilder.Services.AddScoped(serviceImplementInterface, (serviceProvider) =>
								{
									return serviceProvider.GetRequiredService(bxServcieType);
								});
							}
						}
						break;
					case ServiceLifeCycle.Singleton:
						{
							foreach (var serviceImplementInterface in serviceImplementInterfaces)
							{
								appBuilder.Services.AddSingleton(serviceImplementInterface, (serviceProvider) =>
								{
									return serviceProvider.GetRequiredService(bxServcieType);
								});
							}
						}
						break;
				}
			}
		}


		////////////////////////////////////////////////
		// 3/，收集到类型：IWebSocketService。
		////////////////////////////////////////////////
		// !!!
		var bxWebSocketServiceTypes = allTypes.ToTypesWithBaseInterfaceSpecified(typeof(IWebSocketService));
		BrServiceTypesRegistered.IWebSocketServiceTypes = [.. bxWebSocketServiceTypes];
		// !!!
		var webSocketServiceType = typeof(IWebSocketService);
		foreach (var bxWebSocketServiceType in bxWebSocketServiceTypes)
		{
			if (!bxWebSocketServiceType.IsAssignableTo(webSocketServiceType))
			{
				continue;
			}
			var serviceAttribute = bxWebSocketServiceType.GetCustomAttribute<ServiceAttribute>();
			var serviceLifeCycle = ServiceLifeCycle.Scoped;
			if (serviceAttribute != null)
			{
				serviceLifeCycle = serviceAttribute.LifeCycle;
			}


			////////////////////////////////////////////////
			// 1/2，注册服务自身的类型实现：
			////////////////////////////////////////////////
			{
				switch (serviceLifeCycle)
				{
					case ServiceLifeCycle.Transient:
						{
							appBuilder.Services.AddTransient(bxWebSocketServiceType, bxWebSocketServiceType);
						}
						break;
					case ServiceLifeCycle.Scoped:
						{
							appBuilder.Services.AddScoped(bxWebSocketServiceType, bxWebSocketServiceType);
						}
						break;
					case ServiceLifeCycle.Singleton:
						{
							appBuilder.Services.AddSingleton(bxWebSocketServiceType, bxWebSocketServiceType);
						}
						break;
				}
			}


			////////////////////////////////////////////////
			// 2/2，注册服务实现接口的类型实现：
			////////////////////////////////////////////////
			var serviceImplementInterfaces = bxWebSocketServiceType.GetInterfaces();
			if (serviceImplementInterfaces?.Length > 0)
			{
				switch (serviceLifeCycle)
				{
					case ServiceLifeCycle.Transient:
						{
							foreach (var serviceImplementInterface in serviceImplementInterfaces)
							{
								appBuilder.Services.AddTransient(serviceImplementInterface, (serviceProvider) =>
								{
									return serviceProvider.GetRequiredService(bxWebSocketServiceType);
								});
							}
						}
						break;
					case ServiceLifeCycle.Scoped:
						{
							foreach (var serviceImplementInterface in serviceImplementInterfaces)
							{
								appBuilder.Services.AddScoped(serviceImplementInterface, (serviceProvider) =>
								{
									return serviceProvider.GetRequiredService(bxWebSocketServiceType);
								});
							}
						}
						break;
					case ServiceLifeCycle.Singleton:
						{
							foreach (var serviceImplementInterface in serviceImplementInterfaces)
							{
								appBuilder.Services.AddSingleton(serviceImplementInterface, (serviceProvider) =>
								{
									return serviceProvider.GetRequiredService(bxWebSocketServiceType);
								});
							}
						}
						break;
				}
			}
		}

		return appBuilder;
	}

	#endregion
}