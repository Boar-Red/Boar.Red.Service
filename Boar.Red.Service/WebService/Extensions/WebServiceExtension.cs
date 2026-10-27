using BaoXia.Utils.Extensions;
using BaoXia.Utils.Interfaces;
using Boar.Red.Service.Constants;
using Boar.Red.Service.Filters;
using Boar.Red.Service.Utils;
using Boar.Red.Service.WebService.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Boar.Red.Service.WebService.Extensions;

public static class WebServiceExtension
{
	////////////////////////////////////////////////
	// “WebApplicationBuilder”的“WebService”扩展方法。
	////////////////////////////////////////////////

	#region “WebApplicationBuilder”的“WebService”扩展方法。

	public static WebApplicationBuilder ConfigureWebService(
		this WebApplicationBuilder webApplicationBuilder, HttpServer httpServer, WebServiceConfig? webServiceConfig,
		//
		bool isControllersWithViewsServiceEnable, JsonSerializerOptions? jsonSerializerOptionsOfMvc,
		//
		ILogFile? logFile, ILogFile? exceptionLogFile, ILogFile? warningLogFile)
	{
		////////////////////////////////////////////////
		// 1/，基础Web服务信息：
		////////////////////////////////////////////////

		switch (httpServer)
		{
			default:
			case HttpServer.Kestrel:
				{
					var maxRequestBodySize = long.MaxValue;
					if (
						webServiceConfig?.HttpRequestBodySizeInBytesMax
							is long httpRequestBodySizeInBytesMax
						&& httpRequestBodySizeInBytesMax > 0
					)
					{
						maxRequestBodySize = httpRequestBodySizeInBytesMax;
					}
					webApplicationBuilder.WebHost.ConfigureKestrel(kestrelOptions =>
					{
						kestrelOptions.Limits.MaxRequestBodySize = maxRequestBodySize;

						kestrelOptions.Limits.MaxConcurrentConnections = null;
						kestrelOptions.Limits.MaxConcurrentUpgradedConnections = null;

						//kestrelOptions.Limits.RequestHeadersTimeout = null;
						kestrelOptions.Limits.MaxRequestBodySize = null;
						kestrelOptions.Limits.MinRequestBodyDataRate = null;
						// new MinDataRate(bytesPerSecond: 100,
						// gracePeriod: TimeSpan.FromSeconds(10));

						kestrelOptions.Limits.MinResponseDataRate = null;
						// new MinDataRate(bytesPerSecond: 100,
						// gracePeriod: TimeSpan.FromSeconds(10));

						//kestrelOptions.Limits.KeepAliveTimeout = null;

						// 均使用配置文件。
						if (webServiceConfig == null)
						{
							return;
						}

						var httpIpAddress = IPAddress.IPv6Any;
						if (!string.IsNullOrEmpty(webServiceConfig.HttpIPAddress)
						&& IPAddress.TryParse(webServiceConfig.HttpIPAddress, out var httpIPAddressInConfig))
						{
							httpIpAddress = httpIPAddressInConfig;
						}
						var httpPort = webServiceConfig.HttpPort;
						if (httpPort <= 0)
						{
							httpPort = 80;
						}
						////////////////////////////////////////////////
						///
						ServiceConsoleUtil.WriteServiceInfes($"Http服务，开始监听：{httpIpAddress}:{httpPort}");
						///
						////////////////////////////////////////////////
						kestrelOptions.Listen(httpIpAddress, httpPort);

						HttpsCertificateFileInfo[]? originalHttpsCertificateFileInfes = webServiceConfig.HttpsCertificateFileInfes;
						var httpsCertificateFilePath = webServiceConfig.HttpsCertificateFilePath;
						if (string.IsNullOrEmpty(httpsCertificateFilePath) == false)
						{
							var defaultHttpsCertificateFileInfo = new HttpsCertificateFileInfo()
							{
								CertificateFilePath =
										httpsCertificateFilePath,
								CertificatePasswordFilePath =
										webServiceConfig.HttpsCertificatePasswordFilePath,
								CertificateFilePassword =
										webServiceConfig.HttpsCertificateFilePassword,
							};
							if (originalHttpsCertificateFileInfes == null)
							{
								originalHttpsCertificateFileInfes = [defaultHttpsCertificateFileInfo];
							}
							else
							{
								originalHttpsCertificateFileInfes = originalHttpsCertificateFileInfes.ArrayByInsertAt(
									0, defaultHttpsCertificateFileInfo);
							}
						}

						////////////////////////////////////////////////
						// !!!⚠ 注意，程序启动时，只有指定了任意的Https证书， ⚠!!!
						// !!!⚠ 系统才会开启Https服务，                                    ⚠!!!
						// !!!⚠ 可以通过不指定证书的方式，不启用Https服务）。 ⚠!!!
						////////////////////////////////////////////////

						if (originalHttpsCertificateFileInfes?.Length > 0)
						{
							////////////////////////////////////////////////
							// !!!
							WebServiceConfig.SetHttpsCertificateFileInfesCache(originalHttpsCertificateFileInfes, exceptionLogFile);
							// !!!
							////////////////////////////////////////////////

							var httpsIpAddress = IPAddress.IPv6Any;
							if (!string.IsNullOrEmpty(webServiceConfig.HttpsIPAddress)
								&& IPAddress.TryParse(webServiceConfig.HttpsIPAddress, out var httpsIPAddressInConfig))
							{
								httpsIpAddress = httpsIPAddressInConfig;
							}
							var httpsPort = webServiceConfig.HttpsPort;
							if (httpsPort <= 0)
							{
								httpsPort = 443;
							}

							////////////////////////////////////////////////
							///
							var httpsServiceBootInfes = new List<string>
							{
										$"Https服务，开始监听：{httpsIpAddress}:{httpsPort}",
										$"当前已配置Https证书：",
							};
							foreach (var httpsCertificateFileInfo in originalHttpsCertificateFileInfes)
							{
								if (httpsCertificateFileInfo.HostName is { } hostName)
								{
									httpsServiceBootInfes.Add(hostName);
								}
							}
							ServiceConsoleUtil.WriteServiceInfes([.. httpsServiceBootInfes]);
							///
							////////////////////////////////////////////////
							kestrelOptions.Listen(httpsIpAddress, httpsPort, listenOptions =>
							{
								listenOptions.UseHttps(httpsOptions =>
								{
									////////////////////////////////////////////////
									httpsOptions.ServerCertificateSelector = (context, serverName) =>
									{
										var httpsCertificateFileInfes = WebServiceConfig.GetHttpsCertificateFileInfesCache();
										if (httpsCertificateFileInfes == null || httpsCertificateFileInfes.Length < 1)
										{
											return null;
										}
										if (httpsCertificateFileInfes.Length == 1)
										{
											return httpsCertificateFileInfes[0].X509Certificate2;
										}
										for (var httpsCertificateFileInfoIndex = 0;
											httpsCertificateFileInfoIndex < httpsCertificateFileInfes.Length;
											httpsCertificateFileInfoIndex++)
										{
											var validHttpsCertificateFileInfo = httpsCertificateFileInfes[httpsCertificateFileInfoIndex];
											if (serverName.EqualsIgnoreCase(validHttpsCertificateFileInfo.HostName) != true)
											{
												continue;
											}
											return validHttpsCertificateFileInfo.X509Certificate2!;
										}
										return null;
									};
									////////////////////////////////////////////////
								});
							});
						}
						else
						{
							////////////////////////////////////////////////
							///
							ServiceConsoleUtil.WriteServiceInfes("Https服务，未启动：当前没有配置有效的Https证书。");
							///
							////////////////////////////////////////////////
						}
					});
				}
				break;
			case HttpServer.IIS:
				{
					webApplicationBuilder.WebHost.UseIIS();
				}
				break;
		}

		////////////////////////////////////////////////
		// 2/，跨域控制信息：
		////////////////////////////////////////////////
		webApplicationBuilder.ConfigureCrossDomainControl(() =>
		{
			return webServiceConfig?.HttpCrossDomainControlInfo;
		},
		logFile, exceptionLogFile, warningLogFile);

		////////////////////////////////////////////////
		// 2/，根据需要增加MVC服务：
		////////////////////////////////////////////////
		if (isControllersWithViewsServiceEnable)
		{
			var mvcBuilder = webApplicationBuilder.Services.AddControllersWithViews((config) =>
			{
				config.Filters.Add(new TokenCheckerFilter());
				config.Filters.Add(new RequestValidCheckerFilter(exceptionLogFile, webServiceConfig?.HttpHeaderKeysForValueNecessary));
			}).AddControllersAsServices();
			if (mvcBuilder != null && jsonSerializerOptionsOfMvc != null)
			{
				mvcBuilder.AddJsonOptions((mvcJsonOption) =>
				{
					var mvcJsonSerializerOptions =
						mvcJsonOption.JsonSerializerOptions;
					{
						mvcJsonSerializerOptions.SetPropertiesWithSameNameFrom(jsonSerializerOptionsOfMvc);
						if (jsonSerializerOptionsOfMvc.Converters.Count > 0)
						{
							foreach (var jsonConverter in jsonSerializerOptionsOfMvc.Converters)
							{
								//
								mvcJsonSerializerOptions.Converters.Add(jsonConverter);
								//
							}
						}
					}
				});
			}
		}

		return webApplicationBuilder;
	}

	#endregion


	////////////////////////////////////////////////
	// “IApplicationBuilder”的“WebSocketService”扩展方法。
	////////////////////////////////////////////////

	#region “IApplicationBuilder”的“WebSocketService”扩展方法。

	public static IApplicationBuilder UseWebSocketService(this IApplicationBuilder application, WebServiceConfig? webServiceConfig)
	{
		if (webServiceConfig == null)
		{
			return application;
		}
		var webSocketOptions = new WebSocketOptions();
		{
			var webSocketCrossDomainControlInfo = webServiceConfig.WebSocketCrossDomainControlInfo;
			if (webSocketCrossDomainControlInfo != null)
			{
				var accessControlAllowOrigins = webSocketCrossDomainControlInfo.Access_Control_Allow_Origins;
				if (accessControlAllowOrigins?.Length > 0)
				{
					foreach (var allowOrigin in accessControlAllowOrigins)
					{
						webSocketOptions.AllowedOrigins.Add(allowOrigin);
					}
				}
			}

			var keepAliveIntervalSeconds = webServiceConfig.WebSocketKeepAliveIntervalSeconds;
			if (keepAliveIntervalSeconds > 0)
			{
				webSocketOptions.KeepAliveInterval = TimeSpan.FromSeconds(keepAliveIntervalSeconds);
			}
			var keepAliveTimeoutSeconds = webServiceConfig.WebSocketKeepAliveTimeoutSeconds;
			if (double.IsFinite(keepAliveTimeoutSeconds) && keepAliveTimeoutSeconds > 0)
			{
				// Ping/Pong超时会中断失效连接，避免主动Pong持续积压在发送锁上。
				webSocketOptions.KeepAliveTimeout = TimeSpan.FromSeconds(keepAliveTimeoutSeconds);
			}
		}
		application.UseWebSockets(webSocketOptions);

		return application;
	}

	#endregion
}
