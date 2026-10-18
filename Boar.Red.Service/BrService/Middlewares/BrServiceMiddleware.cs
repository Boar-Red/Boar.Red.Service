using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils;
using BaoXia.Utils.ConcurrentTools;
using BaoXia.Utils.Constants;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Interfaces;
using Boar.Red.Service.Attributes;
using Boar.Red.Service.BrService.Attributes;
using Boar.Red.Service.BrService.Constants;
using Boar.Red.Service.BrService.Controllers;
using Boar.Red.Service.BrService.Extensions;
using Boar.Red.Service.BrService.Interfaces;
using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.BrService.Models.StatisticsInfo;
using Boar.Red.Service.BrService.ViewModels;
using Boar.Red.Service.Constants;
using Boar.Red.Service.Filters;
using Boar.Red.Service.Models;
using Boar.Red.Service.Services;
using Boar.Red.Service.Utils;
using Boar.Red.Service.ViewModels;
using Boar.Red.Service.WebService.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;

namespace Boar.Red.Service.BrService.Middlewares;

public class BrServiceMiddleware : MiddlewareAsync
{
	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

	private static int _totalRequestsCountReceived = 0;
	private static int _totalResponsesCountSend = 0;
	private static int _totalSocketsToClientCountAccepted = 0;
	private static int _totalSocketsToClientCountClosed = 0;

	#endregion


	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	protected ILogFile? BrServiceLogFile { get; set; }

	private readonly ConfigInfoInConfigFile<WebServiceConfig> _webServiceConfigInfo;

	public WebServiceConfig WebServiceConfig => _webServiceConfigInfo.Config;

	private readonly ConfigInfoInConfigFile<ServiceConfig> _bxServiceConfigInfo;

	public ServiceConfig Config
	{
		get { return _bxServiceConfigInfo.Config; }
	}

	public ServiceNodeInfo ServiceNodeInfo
	{
		get
		{
			var webServiceConfig = this.WebServiceConfig;
			var bxServiceConfig = this.Config;
			var bxServiceInfo = bxServiceConfig.ServiceInfo;

			string? httpIpAddress = webServiceConfig.HttpIPAddress;
			var httpPort = webServiceConfig.HttpPort;
			string? httpsIpAddress = webServiceConfig.HttpsIPAddress;
			var httpsPort = webServiceConfig.HttpsPort;
			if (bxServiceInfo != null)
			{
				if (!string.IsNullOrWhiteSpace(bxServiceInfo.HttpIPAddress))
				{
					httpIpAddress = bxServiceInfo.HttpIPAddress;
				}
				if (!string.IsNullOrWhiteSpace(bxServiceInfo.HttpsIPAddress))
				{
					httpsIpAddress = bxServiceInfo.HttpsIPAddress;
				}
			}
			var serviceNodeInfo = new ServiceNodeInfo(
				ServiceNodeType.ServiceNode, bxServiceInfo?.GroupName, bxServiceInfo?.ServiceName,
				bxServiceInfo?.NodeName, bxServiceInfo?.ServiceVersion,
				httpIpAddress, httpPort, httpsIpAddress, httpsPort,
				_serviceDiscoverer.ListenPort, _serviceDiscoverer.ListenEndPoint,
				State, StartTime, LoadRate);
			{ }
			return serviceNodeInfo;
		}
	}

	public ApplicationInfo[]? ApplicationInfes
	{
		get { return Config.ApplicationInfes; }
	}

	protected ServiceState _state = ServiceState.Offline;
	public ServiceState State
	{
		get { return _state; }
		private set { _state = value; }
	}

	/// <summary>
	/// 服务开始运行时间。
	/// </summary>
	public DateTime StartTime { get; set; }

	/// <summary>
	/// 服务运行的持续时间。
	/// </summary>
	public TimeSpan StartDuration
	{
		get
		{
			var startDuration = DateTime.Now - this.StartTime;
			{ }
			return startDuration;
		}
	}

	public bool IsServiceShuttingDown
	{
		get
		{
			switch (State)
			{
				case ServiceState.InBooting:
				case ServiceState.InService:
					{
						return false;
					}
				case ServiceState.Offline:
				case ServiceState.InShuttingDown:
				case ServiceState.InShutdown:
					{
						return true;
					}
			}
			return true;
		}
	}

	public double LoadRate { get; set; }

	protected bool _isMvcControllerDefaultBeBrServiceController;
	public bool IsMvcControllerDefaultBeBrServiceController => _isMvcControllerDefaultBeBrServiceController;

	protected readonly Type[] _controllerTypesOrderByControllerNameAsc;

	public Type[] ControllerTypes => _controllerTypesOrderByControllerNameAsc;

	protected WebSocketServiceTypeIndexInfo[]? _webSocketServiceTypeIndexInfosOrderByRequestPathAsc = null;

	protected readonly Lock _webSocketServicesOrderByRequestPathAscLock = new();

	public WebSocketServiceTypeIndexInfo[] WebSocketServiceTypeIndexInfosOrderByRequestPathAsc
		=> _webSocketServiceTypeIndexInfosOrderByRequestPathAsc ?? [];


	protected RequestValidCheckerFilter? _requestValidCheckerFilter = null;

	protected readonly Lock _requestValidCheckerFilterLock = new();

	public RequestValidCheckerFilter RequestValidCheckerFilter
	{
		get
		{
			if (_requestValidCheckerFilter == null)
			{
				lock (_requestValidCheckerFilterLock)
				{
					_requestValidCheckerFilter ??= new RequestValidCheckerFilter(
						null, _webServiceConfigInfo.Config.HttpHeaderKeysForValueNecessary);
				}
			}
			return _requestValidCheckerFilter;
		}
	}


	protected TokenCheckerFilter? _tokenCheckerFilter = null;

	protected readonly Lock _tokenCheckerFilterLock = new();

	public TokenCheckerFilter TokenCheckerFilter
	{
		get
		{
			if (_tokenCheckerFilter == null)
			{
				lock (_tokenCheckerFilterLock)
				{
					_tokenCheckerFilter ??= new TokenCheckerFilter();
				}
			}
			return _tokenCheckerFilter;
		}
	}


	protected readonly List<RouteTemplate> _routeTemplates = [];
	public List<RouteTemplate> RouteTemplates => _routeTemplates;


	protected readonly ConcurrentQueue<ApiRequestInfoBytesInfo> _requestInfoBytesInfesNeedProcessed = new();

	protected Tasks _tasksToProcessRequestInfes;

	protected LoopTask _taskToSendHeartbeat;

	public bool IsServiceIdle
	{
		get
		{
			// 是否存在：等待处理的请求信息。
			if (_requestInfoBytesInfesNeedProcessed.IsEmpty != true)
			{
				return false;
			}
			var clientConnectionInfes = BrClientConnectionInfoController.ClientConnectionInfes;
			foreach (var clientConnectionInfo in clientConnectionInfes)
			{
				// 是否存在：等待发送的响应信息。
				if (clientConnectionInfo.ResponseInfesNeedSend.IsEmpty != true)
				{
					return false;
				}
				// 是否存在：还在连接中的Socket。
				//if (clientConnectionInfo.IsAnySocketConnected)
				//{
				//	return false;
				//}
			}
			return true;
		}
	}

	public bool IsServiceDisconnected
	{
		get
		{
			if (!IsServiceIdle)
			{
				return false;
			}
			var clientConnectionInfes = BrClientConnectionInfoController.ClientConnectionInfes;
			foreach (var clientConnectionInfo in clientConnectionInfes)
			{
				// 是否存在：还在连接中的Socket。
				if (clientConnectionInfo.IsAnySocketConnected)
				{
					return false;
				}
			}
			return true;
		}
	}

	protected readonly ConcurrentQueue<ServiceRequestPipeTimeInfo> _requestPipeTimeInfes = new();

	protected readonly List<ServiceRequestStatisticsInfo> _requestStatisticsInfesOrderedByRequestTimeAsc = [];

	public ServiceRequestStatisticsInfo[] RequestStatisticsInfesOrderedByRequestTimeAsc { get; set; } = [];

	protected readonly LoopTask _taskToPolymerizeRequestPipeTimeInfes;

	public CancellationTokenSource CancellationTokenSource { get; set; } = new();

	protected ServiceDiscoverer _serviceDiscoverer;

	public ServiceDiscoverer ServiceDiscoverer
	{
		get { return _serviceDiscoverer; }
		set
		{
			if (value == null)
			{
				return;
			}
			if (_serviceDiscoverer == value)
			{
				return;
			}

			_serviceDiscoverer.CurrentBrService = null;
			{
				_serviceDiscoverer = value;
			}
			_serviceDiscoverer.CurrentBrService = this;
		}
	}

	public ServiceInfesManager? ServiceFoundInfesManager
	{
		get { return ServiceDiscoverer.ServiceFoundInfesManager; }
	}
	public Action<Action<string, double>, CancellationToken>? ToServiceBooting { get; set; }
	public Action<CancellationToken>? ToServiceStarted { get; set; }
	public Action<CancellationToken>? ToServiceShutdown { get; set; }
	public Func<string?, bool>? ToIsClientVersionSupported { get; set; }
	public Func<double>? ToGetServiceLoadRate { get; set; }
	public Action<ServiceRequestStatisticsInfo[]>? ToSaveServiceRequestStatisticsInfes { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	protected static bool TryToCreateControllerMethodParamFromRequestMethodParams(
		JsonElement requestMethodParams, IEnumerable<DataPacketSection>? requestMethodExtensionParams,
		ParameterInfo? controllerMethodParamInfo, ref List<object?>? requestMethodParamValueList)
	{
		if (controllerMethodParamInfo == null)
		{
			return false;
		}

		string? controllerMethodParamName = controllerMethodParamInfo.Name;
		if (string.IsNullOrEmpty(controllerMethodParamName))
		{
			return false;
		}

		Type controllerMethodParamType = controllerMethodParamInfo.ParameterType;
		//try
		//{
		if (requestMethodParams.ValueKind != JsonValueKind.Undefined)
		{
			var methodSingleParam = requestMethodParams.Deserialize(
				controllerMethodParamType,
				BaoXia.Utils.Environment.JsonSerializerOptions
			);
			// 检查补充，二进制字段：
			if (methodSingleParam != null)
			{
				var methodParamPropertyInfes =
					methodSingleParam.GetPublicSettablePropertyInfes();
				if (methodParamPropertyInfes?.Length > 0)
				{
					foreach (
						var methodParamPropertyInfo in methodParamPropertyInfes
					)
					{
						var methodParamPropertyType =
							methodParamPropertyInfo.PropertyType;
						if (methodParamPropertyType.Equals(typeof(byte[])))
						{
							var requestMethodExtensionParam =
								requestMethodExtensionParams?.GetBytesDataSectionWithName(
									controllerMethodParamName
								);
							if (requestMethodExtensionParam == null)
							{
								return false;
							}

							// !!!
							var methodParamPropertyValue_Bytes =
								requestMethodExtensionParam;
							// !!!
							methodParamPropertyInfo.SetValue(
								methodSingleParam,
								methodParamPropertyValue_Bytes
							);
							// !!!
						}
						else if (
							methodParamPropertyType.IsAssignableTo(
								typeof(IList<byte>)
							)
						)
						{
							var requestMethodExtensionParam =
								requestMethodExtensionParams?.GetBytesDataSectionWithName(
									controllerMethodParamName
								);
							if (requestMethodExtensionParam == null)
							{
								return false;
							}

							// !!!
							var methodParamPropertyValue_ByteList =
								methodParamPropertyType.IsInterface
									? []
									: Activator.CreateInstance(
										methodParamPropertyType
									) as IList<byte>;
							var requestMethodExtensionParamBytes =
								requestMethodExtensionParam.BytesValue;
							if (
								requestMethodExtensionParamBytes
								!= null
							)
							{
								if (
									methodParamPropertyValue_ByteList
									is List<byte> byteList
								)
								{
									byteList.AddRange(
										requestMethodExtensionParamBytes
									);
								}
								else if (
									methodParamPropertyValue_ByteList
									!= null
								)
								{
									foreach (
										var requestMethodExtensionParamByte in requestMethodExtensionParamBytes
									)
									{
										methodParamPropertyValue_ByteList.Add(
											requestMethodExtensionParamByte
										);
									}
								}
							}
							// !!!
							methodParamPropertyInfo.SetValue(
								methodSingleParam,
								methodParamPropertyValue_ByteList
							);
							// !!!
						}
						else if (
							methodParamPropertyType.Equals(
								typeof(FormFile)
							)
							|| methodParamPropertyType.Equals(
								typeof(IFormFile)
							)
						)
						{
							var requestMethodExtensionParam =
								requestMethodExtensionParams?.GetBytesDataSectionWithName(
									controllerMethodParamName
								);
							if (requestMethodExtensionParam == null)
							{
								return false;
							}

							// !!!
							IFormFile? methodParamPropertyValue_IFormFile =
								null;
							var requestMethodExtensionParamBytes =
								requestMethodExtensionParam.BytesValue;
							if (
								requestMethodExtensionParamBytes
								!= null
							)
							{
								methodParamPropertyValue_IFormFile =
									new FormFile(
										new MemoryStream(
											requestMethodExtensionParamBytes
										),
										0,
										requestMethodExtensionParamBytes.Length,
										controllerMethodParamName,
										controllerMethodParamName
									);
							}
							// !!!
							methodParamPropertyInfo.SetValue(
								methodSingleParam,
								methodParamPropertyValue_IFormFile
							);
							// !!!
						}
					}
				}
			}

			requestMethodParamValueList ??= [];
			requestMethodParamValueList.Add(methodSingleParam);

			////////////////////////////////////////////////
			// !!! ⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠
			return true;
			// !!! ⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠
			////////////////////////////////////////////////
		}
		else if (controllerMethodParamInfo?.HasDefaultValue == true)
		{
			requestMethodParamValueList ??= [];
			requestMethodParamValueList.Add(controllerMethodParamInfo.DefaultValue);

			////////////////////////////////////////////////
			// !!! ⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠
			return true;
			// !!! ⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠
			////////////////////////////////////////////////
		}
		//}
		//catch (Exception exception)
		//{
		//	////////////////////////////////////////////////
		//	// !!! 不隐藏请求参数的反序列化异常。 !!!
		//	////////////////////////////////////////////////
		//}
		return false;
	}

	protected static bool TryToGetControllerMethodParamFromApiRequestInfoWithParamType(
		Type controllerMethodParamType, string? controllerMethodParamName,
		ApiRequestInfo<JsonElement>? apiRequestInfo, ref List<object?>? requestMethodParamValueList)
	{
		if (string.IsNullOrEmpty(controllerMethodParamName))
		{
			return false;
		}
		if (apiRequestInfo == null)
		{
			return false;
		}

		if (controllerMethodParamType.Equals(typeof(byte[])))
		{
			var requestMethodExtensionParams = apiRequestInfo.MethodExtensionParams;
			if (requestMethodExtensionParams == null)
			{
				return false;
			}

			var requestMethodExtensionParam =
				requestMethodExtensionParams.GetBytesDataSectionWithName(
					controllerMethodParamName
				);
			if (requestMethodExtensionParam == null)
			{
				return false;
			}

			////////////////////////////////////////////////
			var requestMethodParamValue = requestMethodExtensionParam.BytesValue;

			requestMethodParamValueList ??= [];
			requestMethodParamValueList.Add(requestMethodParamValue);

			// !!! ⚠
			return true;
			// !!! ⚠
			////////////////////////////////////////////////
		}
		else if (controllerMethodParamType.IsAssignableTo(typeof(IList<byte>)))
		{
			var requestMethodExtensionParams = apiRequestInfo.MethodExtensionParams;
			if (requestMethodExtensionParams == null)
			{
				return false;
			}

			var requestMethodExtensionParam =
				requestMethodExtensionParams.GetBytesDataSectionWithName(
					controllerMethodParamName
				);
			if (requestMethodExtensionParam == null)
			{
				return false;
			}

			////////////////////////////////////////////////
			IList<byte>? requestMethodParamValue = null;
			var requestMethodExtensionParamBytes =
				requestMethodExtensionParam.BytesValue;
			if (requestMethodExtensionParamBytes != null)
			{
				requestMethodParamValue = controllerMethodParamType.IsInterface
					? []
					: Activator.CreateInstance(controllerMethodParamType)
						as IList<byte>;
				if (requestMethodParamValue is List<byte> byteList)
				{
					byteList.AddRange(requestMethodExtensionParamBytes);
				}
				else if (requestMethodParamValue != null)
				{
					foreach (
						var requestMethodExtensionParamByte in requestMethodExtensionParamBytes
					)
					{
						requestMethodParamValue.Add(
							requestMethodExtensionParamByte
						);
					}
				}
			}

			requestMethodParamValueList ??= [];
			requestMethodParamValueList.Add(requestMethodParamValue);

			// !!! ⚠
			return true;
			// !!! ⚠
			////////////////////////////////////////////////
		}
		else if (
			controllerMethodParamType.Equals(typeof(FormFile))
			|| controllerMethodParamType.Equals(typeof(IFormFile))
		)
		{
			var requestMethodExtensionParams = apiRequestInfo.MethodExtensionParams;
			if (requestMethodExtensionParams == null)
			{
				return false;
			}

			var requestMethodExtensionParam =
				requestMethodExtensionParams.GetBytesDataSectionWithName(
					controllerMethodParamName
				);
			if (requestMethodExtensionParam == null)
			{
				return false;
			}

			////////////////////////////////////////////////
			IFormFile? requestMethodParamValue = null;
			var requestMethodExtensionParamBytes =
				requestMethodExtensionParam.BytesValue;
			if (requestMethodExtensionParamBytes != null)
			{
				requestMethodParamValue = new FormFile(
					new MemoryStream(requestMethodExtensionParamBytes),
					0,
					requestMethodExtensionParamBytes.Length,
					controllerMethodParamName,
					controllerMethodParamName
				);
			}

			requestMethodParamValueList ??= [];
			requestMethodParamValueList.Add(requestMethodParamValue);

			// !!! ⚠
			return true;
			// !!! ⚠
			////////////////////////////////////////////////
		}
		else
		{
			var requestMethodParams = apiRequestInfo.MethodParams;
			JsonElement.ObjectEnumerator? requestMethodParamKeyValues = null;
			if (requestMethodParams.ValueKind == JsonValueKind.Object)
			{
				requestMethodParamKeyValues = requestMethodParams.GetProperties();
			}
			if (requestMethodParamKeyValues?.IsNotEmpty() != true)
			{
				return false;
			}

			foreach (var requestMethodParam in requestMethodParamKeyValues)
			{
				if (
					requestMethodParam.Name.EqualsIgnoreCase(
						controllerMethodParamName
					) == true
				)
				{
					////////////////////////////////////////////////
					var requestMethodParamValue =
						requestMethodParam.Value.ToObject(
							controllerMethodParamType
						);
					requestMethodParamValueList ??= [];
					requestMethodParamValueList.Add(requestMethodParamValue);

					// !!! ⚠
					return true;
					// !!! ⚠
					////////////////////////////////////////////////
				}
			}
		}

		return false;
	}

	public static string CreateRequestPathKeyWithRequestPath(string? requestPath)
	{
		var requestPathKey = requestPath ?? string.Empty;
		if (requestPathKey.Length > 0)
		{
			requestPathKey = requestPathKey.Replace('\\', '/');
			requestPathKey = requestPathKey.Replace("//", "/");
			requestPathKey = requestPathKey.TrimEnd('/');
			if (!requestPathKey.StartsWith('/'))
			{
				requestPathKey = '/' + requestPathKey;
			}
		}
		return requestPathKey;
	}

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public BrServiceMiddleware(
		bool isMvcControllerDefaultBeBrServiceController,
		Dictionary<string, string>? serviceRouteTempaltes,
		//
		ConfigFile? webServiceConfigFile,
		string? webServiceConfigPropertyNameInConfigFile,
		ConfigFile? bxServiceConfigFile,
		string? bxServiceConfigPropertyNameInConfigFile,
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
		: base(logFile, exceptionLogFile, warningLogFile)
	{
		BrServiceLogFile = bxServiceLogFile;

		_isMvcControllerDefaultBeBrServiceController = isMvcControllerDefaultBeBrServiceController;


		var controllerTypes = DidCreateControllerTypes();
		if (controllerTypes != null)
		{
			_controllerTypesOrderByControllerNameAsc = [.. controllerTypes];
			_controllerTypesOrderByControllerNameAsc.Sort((controllerTypeA, controllerTypeB) =>
			{
				return controllerTypeA.Name.CompareTo(controllerTypeB.Name);
			});
		}
		else
		{
			_controllerTypesOrderByControllerNameAsc = [];
		}


		if (serviceRouteTempaltes == null || serviceRouteTempaltes.Count < 1)
		{
			serviceRouteTempaltes = RouteTemplateConstants.RouteTemplatesDefault;
		}
		foreach (var serviceRouteTempalte in serviceRouteTempaltes)
		{
			var routeTemplate = new RouteTemplate(serviceRouteTempalte.Key, serviceRouteTempalte.Value);
			//
			_routeTemplates.Add(routeTemplate);
			//
		}


		_webServiceConfigInfo = new ConfigInfoInConfigFile<WebServiceConfig>(
			webServiceConfigFile, webServiceConfigPropertyNameInConfigFile, WebServiceConfig.Default, DidConfigFileChangedAsync);
		_bxServiceConfigInfo = new ConfigInfoInConfigFile<ServiceConfig>(
			bxServiceConfigFile, bxServiceConfigPropertyNameInConfigFile, ServiceConfig.Default, DidConfigFileChangedAsync);


		////////////////////////////////////////////////
		var serviceConfig = Config;
		{
			_serviceDiscoverer = serviceDiscoverSpecified ?? new ServiceDiscoverer();
			// !!!
			_serviceDiscoverer.CurrentBrService = this;
			// !!!
		}

		////////////////////////////////////////////////

		_tasksToProcessRequestInfes = new(() =>
		{
			var requestProcessThreadsCountMax = Config.RequestProcessThreadsCountMax;
			{ }
			return requestProcessThreadsCountMax;
		});

		_taskToSendHeartbeat = new((cancellationToken) =>
		{
			// !!!
			DidSendHeartbeatToApplications(cancellationToken);
			// !!!
			return true;
		},
		() =>
		{
			return Config.SocketConnectionHeartbeatIntervalSecondsFinally;
		});

		_taskToPolymerizeRequestPipeTimeInfes = new LoopTask((cancellationToken) =>
		{
			if (IsServiceShuttingDown)
			{
				return false;
			}

			// !!!
			this.DidPolymerizeRequestPipeTimeInfes();
			// !!!
			return true;
		}, 0.5);

		////////////////////////////////////////////////

		ToServiceBooting = toServiceBooting;
		ToServiceStarted = toServiceStarted;
		ToServiceShutdown = toServiceShutdown;

		ToIsClientVersionSupported = toIsClientVersionSupported;
		ToGetServiceLoadRate = toGetServiceLoadRate;

		ToSaveServiceRequestStatisticsInfes = toSaveServiceRequestStatisticsInfes;
	}

	public ApplicationInfo? GetApplicationInfoWithApplicationIdentity(string? applicationIdentity)
	{
		if (string.IsNullOrEmpty(applicationIdentity))
		{
			return null;
		}
		if (ApplicationInfes is not ApplicationInfo[] applicationInfes)
		{
			return null;
		}

		foreach (var applicationInfo in applicationInfes)
		{
			if (
				applicationInfo.ApplicationIdentity.EqualsIgnoreCase(
					applicationIdentity
				)
			)
			{
				return applicationInfo;
			}
		}
		return null;
	}

	internal void AddServiceRequestPipeTimeInfo(
		ServiceRequestPipeTimeInfo? requestPipeTimeInfo, RequestPipeType requestPipeTypeSpecified = RequestPipeType.Unknown)
	{
		if (requestPipeTimeInfo == null)
		{
			return;
		}
		if (requestPipeTypeSpecified != RequestPipeType.Unknown)
		{
			// !!!
			requestPipeTimeInfo.Type = requestPipeTypeSpecified;
			// !!!
		}
		if (requestPipeTimeInfo.State == RequestPipeState.Unknown)
		{
			// !!!
			requestPipeTimeInfo.State = RequestPipeState.Success;
			// !!!
		}
		_requestPipeTimeInfes.Enqueue(requestPipeTimeInfo);
	}

	/// <summary>
	/// 开始服务。
	/// </summary>
	public void Start()
	{
		////////////////////////////////////////////////
		////////////////////////////////////////////////
		////////////////////////////////////////////////

		// !!!
		State = ServiceState.InBooting;
		StartTime = DateTime.Now;
		{
			this.DidServiceBooting(CancellationTokenSource.Token);
		}
		//
		State = ServiceState.InService;
		{
			this.DidServiceStarted(CancellationTokenSource.Token);
		}
		// !!!

		////////////////////////////////////////////////
		////////////////////////////////////////////////
		////////////////////////////////////////////////
	}

	/// <summary>
	/// 服务关机，流程为：
	/// 1，触发服务关机。
	/// 2，开始关机后，不再接受新的客户端连接。
	/// 3，继续处理，已有连接的客户端请求。
	/// 4，【重要】直到收到客户端的关机请求后，
	///      才会中断"接收"客户端请求。
	/// 5，Socket的关闭应当由，【服务使用者】即【客户端】完成，
	///      已保证服务的事务完整性。
	/// </summary>
	public async Task ShutdownAsync()
	{
		if (IsServiceShuttingDown)
		{
			return;
		}
		lock (this)
		{
			if (IsServiceShuttingDown)
			{
				return;
			}
			// !!!
			State = ServiceState.InShuttingDown;
			// !!!
			// !!!
			DebugInfo.WriteLine(
				"关机测试",
				"服务端",
				$"关机开始，共 {_totalSocketsToClientCountAccepted} 个Socket需要被关闭。"
			);
			// !!!
		}

		var serviceConfig = Config;

		////////////////////////////////////////////////
		// 1/2，关闭心跳包等自动任务：
		////////////////////////////////////////////////
		{
			await _taskToSendHeartbeat.CancelAsync();
			await _taskToPolymerizeRequestPipeTimeInfes.CancelAsync();
		}

		////////////////////////////////////////////////
		// 2/2，向客户端广播关闭消息：
		////////////////////////////////////////////////
		{
			// !!!
			BroadcastResponseToClient<object>(
				ApiRequestIdentity.Service.Shutdown,
				null,
				DataPackageEncryptionMethod.None
			);
			// !!!
		}

		////////////////////////////////////////////////
		// 等待所有已接收的请求处理完毕：
		////////////////////////////////////////////////
		{
			var serviceShutdownTimeoutSeconds =
				serviceConfig.ServiceShutdownTimeoutSeconds;
			var timeoutCancellationTokenSource = new CancellationTokenSource();
			var timeoutCancellationTokenToken = timeoutCancellationTokenSource.Token;
			try
			{
				if (serviceShutdownTimeoutSeconds > 0.0)
				{
					timeoutCancellationTokenSource.CancelAfter(
						(int)(serviceShutdownTimeoutSeconds * 1000.0)
					);
				}

				while (!timeoutCancellationTokenToken.IsCancellationRequested)
				{
					if (IsServiceDisconnected)
					{
						break;
					}
					else
					{
						await Task.Delay((int)(0.1 * 1000));
					}
				}
			}
			catch (Exception exception)
			{
				if (
					exception
						is OperationCanceledException operationCanceledException
					&& operationCanceledException.CancellationToken
						== timeoutCancellationTokenToken
				)
				{
					ExceptionLogFile?.Logs(
						this,
						"服务安全关机失败，关机操作超过了 "
							+ serviceShutdownTimeoutSeconds
							+ " 秒。",
						exception
					);
				}
				else
				{
					ExceptionLogFile?.Logs(
						this,
						"服务安全关机失败，程序异常。",
						exception
					);
				}
			}
			finally
			{
				lock (this)
				{
					////////////////////////////////////////////////
					State = ServiceState.InShutdown;
					DidServiceShutdown(CancellationTokenSource.Token);
					////////////////////////////////////////////////
				}
			}
			// !!!
			DebugInfo.WriteLine(
				"关机测试",
				"服务端",
				$"关机完成，合计关闭Socket {_totalSocketsToClientCountClosed}/{_totalSocketsToClientCountAccepted} 个，合计收到请求 {_totalRequestsCountReceived} 个，响应发送 {_totalResponsesCountSend} 个。"
			);
			// !!!

			// !!! 应用程序关闭 !!!
			if (BaoXia.Utils.Environment.ApplicationBuilder is WebApplication app)
			{
				await app.StopAsync();
			}
		}
	}

	protected Type? GetControllerTypeWithControllerName(string? controllerName)
	{
		if (string.IsNullOrEmpty(controllerName))
		{
			return null;
		}

		string controllerNameWithControllerSuffix = controllerName + Constants.NamingConventions.ControllerNameSuffix;
		var targetControllerTypeInfo = _controllerTypesOrderByControllerNameAsc.FindItemWithDichotomy(true, (controllerTypeInfo, _) =>
		{
			if (controllerTypeInfo.Name.EqualsIgnoreCase(controllerNameWithControllerSuffix))
			{
				return 0;
			}
			return controllerTypeInfo.Name.CompareTo(controllerNameWithControllerSuffix);
		});
		return targetControllerTypeInfo;
	}

	protected Type? GetRequestTargetWebSocketServiceTypeInHttpContext(HttpContext httpContext)
	{
		if (_webSocketServiceTypeIndexInfosOrderByRequestPathAsc == null)
		{
			lock (_webSocketServicesOrderByRequestPathAscLock)
			{
				if (_webSocketServiceTypeIndexInfosOrderByRequestPathAsc == null)
				{
					using var scope = BaoXia.Utils.Environment.CreateServiceScope();
					if (scope != null)
					{
						var webSocketServiceIndexInfosOrderByRequestPathAsc = new List<WebSocketServiceTypeIndexInfo>();
						foreach (var webSocketServiceType in BrServiceTypesRegistered.IWebSocketServiceTypes)
						{
							if (scope.ServiceProvider.GetService(webSocketServiceType) is not IWebSocketService webSocketService)
							{
								continue;
							}
							var webSocketServiceTypeIndexInfo = new WebSocketServiceTypeIndexInfo(
								webSocketService.RequestPath, webSocketService.RequestPathKey, webSocketServiceType);
							webSocketServiceIndexInfosOrderByRequestPathAsc.InsertWithOrder(
								webSocketServiceTypeIndexInfo, (indexInfoA, indexInfoB) =>
							{
								return indexInfoA.RequestPathKey.CompareTo(indexInfoB.RequestPathKey);
							});
						}
						// !!!
						_webSocketServiceTypeIndexInfosOrderByRequestPathAsc = [.. webSocketServiceIndexInfosOrderByRequestPathAsc];
						// !!!
					}
				}
			}
		}
		else if (_webSocketServiceTypeIndexInfosOrderByRequestPathAsc.Length < 1)
		{
			return null;
		}

		var requstPath = BrServiceMiddleware.CreateRequestPathKeyWithRequestPath(httpContext.Request.Path.Value);
		if (string.IsNullOrEmpty(requstPath))
		{
			return null;
		}

		var requestTargetWebSocketServiceTypeIndexInfo = _webSocketServiceTypeIndexInfosOrderByRequestPathAsc
			.FindItemWithDichotomy(true, (webSocketService, _) =>
			{
				return webSocketService.RequestPathKey.CompareTo(requstPath);
			});
		return requestTargetWebSocketServiceTypeIndexInfo?.WebSocketServiceType;
	}

	protected RouteAction? GetRouteActionWithRequestUriPath(string? requestUriPath)
	{
		if (string.IsNullOrEmpty(requestUriPath))
		{
			return null;
		}
		if (_routeTemplates.Count < 1)
		{
			return null;
		}

		RouteInfo? routeInfo = null;
		foreach (var routeTemplate in _routeTemplates)
		{
			routeInfo = routeTemplate.GetRouteInfoWithUrlPath(requestUriPath);
			if (routeInfo != null)
			{
				break;
			}
		}
		if (routeInfo == null || routeInfo.IsValid != true)
		{
			return null;
		}

		var controllerType = this.GetControllerTypeWithControllerName(
			routeInfo.ControllerName
		);
		if (controllerType == null)
		{
			return null;
		}

		var controller =
			BaoXia.Utils.Environment.ApplicationBuilder?.ApplicationServices.GetService(
				controllerType
			);
		if (controller == null)
		{
			return null;
		}

		return new RouteAction(routeInfo, controller);
	}

	protected async Task<ServiceActionResultInfo> GetActionResultWithRouteActionAsync(
		RouteAction? routeAction,
		ApiRequestInfo<JsonElement> apiRequestInfo,
		object? controllerSpecified
	)
	{
		if (routeAction == null || routeAction.IsValid != true)
		{
			// 404
			return new(
				new StatusCodeResult((int)System.Net.HttpStatusCode.NotFound),
				null
			);
		}

		var controllerMatched = controllerSpecified ?? routeAction.Controller;
		var controllerType = controllerMatched.GetType();
		var controllerMethods = controllerType.GetMethods(
			BindingFlags.IgnoreCase
				| BindingFlags.Public
				| BindingFlags.Instance
				| BindingFlags.InvokeMethod
		);

		if (controllerMethods.Length < 1)
		{
			return new(
				new StatusCodeResult((int)System.Net.HttpStatusCode.NotFound),
				null
			);
		}

		var routeActionParams = routeAction.ActionParams;
		var routeActionParamsCountWithoutReservedParams =
			routeAction.ActionParamsCountWithoutReservedParams;

		var actionName = routeAction.ActionName;
		List<MethodInfo>? controllerMethodsInSameActionNameOrderByParametersCountDesc =
			null;
		//foreach (var method in controllerMethods)
		for (
			var controllerMethodIndex = controllerMethods.Length - 1;
			controllerMethodIndex >= 0;
			controllerMethodIndex--
		)
		{
			var controllerMethod = controllerMethods[controllerMethodIndex];
			if (actionName.EqualsIgnoreCase(controllerMethod.Name))
			{
				if (
					controllerMethodsInSameActionNameOrderByParametersCountDesc
					== null
				)
				{
					controllerMethodsInSameActionNameOrderByParametersCountDesc =
					[
						controllerMethod,
					];
				}
				else
				{
					var methodParametersCount = controllerMethod
						.GetParameters()
						.Length;
					var methodInsertIndex =
						controllerMethodsInSameActionNameOrderByParametersCountDesc.Count;
					for (
						var prevMethodIndex = methodInsertIndex - 1;
						prevMethodIndex >= 0;
						prevMethodIndex--
					)
					{
						var prevMethod =
							controllerMethodsInSameActionNameOrderByParametersCountDesc[
								prevMethodIndex
							];
						if (
							methodParametersCount
							<= prevMethod.GetParameters().Length
						)
						{
							methodInsertIndex = prevMethodIndex++;
							break;
						}
					}
					// !!!
					controllerMethodsInSameActionNameOrderByParametersCountDesc.Insert(
						methodInsertIndex,
						controllerMethod
					);
					// !!!
				}
			}
		}

		MethodInfo? controllerMethodMatched = null;
		List<object?>? requestMethodParamValueList = null;
		if (controllerMethodsInSameActionNameOrderByParametersCountDesc?.Count > 0)
		{
			var requestMethodParamKeyValuesCount = 0;
			var requestMethodParams = apiRequestInfo.MethodParams;
			if (requestMethodParams.ValueKind == JsonValueKind.Object)
			{
				requestMethodParamKeyValuesCount =
					requestMethodParams.GetPropertiesCount();
			}
			var requestMethodParamsSources =
				requestMethodParamKeyValuesCount
				>= routeActionParamsCountWithoutReservedParams
					?
					[
						ApiRequestMethodParamsSource.RequestBody,
						ApiRequestMethodParamsSource.UriQuery,
					]
					: new ApiRequestMethodParamsSource[]
					{
						ApiRequestMethodParamsSource.UriQuery,
						ApiRequestMethodParamsSource.RequestBody,
					};
			var requestMethodExtensionParams = apiRequestInfo.MethodExtensionParams;

			foreach (
				var method in controllerMethodsInSameActionNameOrderByParametersCountDesc
			)
			{
				var controllerMethodParams = method.GetParameters();
				var methodSingleParamInfo =
					controllerMethodParams.Length == 1
						? controllerMethodParams[0]
						: null;
				var methodSingleParamType = methodSingleParamInfo?.ParameterType;

				var isAllMethodParamsMatched = true;
				foreach (
					var requestMethodParamsSource in requestMethodParamsSources
				)
				{
					switch (requestMethodParamsSource)
					{
						case ApiRequestMethodParamsSource.RequestBody:
							{
								//
								requestMethodParamValueList?.Clear();
								//
								if (methodSingleParamType != null)
								{
									if (
										TryToCreateControllerMethodParamFromRequestMethodParams(
											requestMethodParams,
											requestMethodExtensionParams,
											methodSingleParamInfo,
											ref requestMethodParamValueList
										)
									)
									{
										////////////////////////////////////////////////
										// !!! ⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠
										controllerMethodMatched =
											method;
										break;
										// !!! ⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠
										////////////////////////////////////////////////
									}
									else
									{
										// !!!
										isAllMethodParamsMatched =
											false;
										break;
										// !!!
									}
								}
								else
								{
									foreach (
										var controllerMethodParam in controllerMethodParams
									)
									{
										////////////////////////////////////////////////
										// !!! ⚠
										var isRequestMethodParamMatched =
											TryToGetControllerMethodParamFromApiRequestInfoWithParamType(
												controllerMethodParam.ParameterType,
												controllerMethodParam.Name,
												apiRequestInfo,
												ref requestMethodParamValueList
											);
										// !!! ⚠
										////////////////////////////////////////////////

										if (
											isRequestMethodParamMatched
												!= true
											&& controllerMethodParam.HasDefaultValue
										)
										{
											requestMethodParamValueList ??=
											[];
											requestMethodParamValueList.Add(
												controllerMethodParam.DefaultValue
											);

											// !!! ⚠
											isRequestMethodParamMatched =
												true;
											// !!! ⚠
										}
										////////////////////////////////////////////////
										if (
											isRequestMethodParamMatched
											!= true
										)
										{
											// !!!
											isAllMethodParamsMatched =
												false;
											break;
											// !!!
										}
									}
								}
							}
							break;
						case ApiRequestMethodParamsSource.UriQuery:
							{
								if (
									routeActionParamsCountWithoutReservedParams
										> 0
									&& routeActionParams != null
								)
								{
									//
									requestMethodParamValueList?.Clear();
									//
									foreach (
										var controllerMethodParam in controllerMethodParams
									)
									{
										var isRequestMethodParamMatched =
											false;
										////////////////////////////////////////////////
										var controllerMethodParamName = controllerMethodParam.Name;
										foreach (var routeActionParam in routeActionParams)
										{
											if (routeActionParam.Key.EqualsIgnoreCase(controllerMethodParamName) == true)
											{
												var controllerMethodParamType = controllerMethodParam.ParameterType;
												var requestMethodParamValue =
													routeActionParam.Value?.ToObjectByJsonDeserialize(controllerMethodParamType);
												requestMethodParamValueList ??= [];
												requestMethodParamValueList.Add(requestMethodParamValue);

												// !!! ⚠
												isRequestMethodParamMatched = true;
												// !!! ⚠
												break;
											}
										}
										if (isRequestMethodParamMatched != true && controllerMethodParam.HasDefaultValue)
										{
											requestMethodParamValueList ??= [];
											requestMethodParamValueList.Add(controllerMethodParam.DefaultValue);

											// !!! ⚠
											isRequestMethodParamMatched = true;
											// !!! ⚠
										}
										////////////////////////////////////////////////
										if (isRequestMethodParamMatched != true)
										{
											// !!!
											isAllMethodParamsMatched = false;
											break;
											// !!!
										}
									}
								}
							}
							break;
					}
					if (isAllMethodParamsMatched == true)
					{
						// !!!
						controllerMethodMatched = method;
						break;
						// !!!
					}
				}
				if (isAllMethodParamsMatched == true)
				{
					// !!!
					controllerMethodMatched = method;
					break;
					// !!!
				}
			}
		}
		if (controllerMethodMatched == null)
		{
			return new(
				new StatusCodeResult((int)System.Net.HttpStatusCode.NotFound),
				null
			);
		}

		var serviceAdministratorMethodAttribute = controllerMethodMatched
			.GetCustomAttributes(typeof(ServiceAdministratorApiAttribute), true)
			.FirstOrDefault();
		if (serviceAdministratorMethodAttribute != null)
		{
			ServiceAdministratorRequest? serviceAdministratorRequest = null;
			if (requestMethodParamValueList?.Count > 0)
			{
				foreach (var requestMethodParamValue in requestMethodParamValueList)
				{
					if (requestMethodParamValue is BaoXia.Constants.Models.Request request)
					{
						if (request.GetErrorResonseByCheckRequest(RequestPipelineType.BxService) is { } errorResponse)
						{
							return new(new JsonResult(errorResponse), null);
						}
						if (request is RequestWithClientIpInfo requestWithClientIpInfo)
						{
							if (apiRequestInfo.ClientIpInfo is { } socketClientIpInfo)
							{
								var clietnIpInfoInRequest = requestWithClientIpInfo.ClientIpInfo;
								if (string.IsNullOrEmpty(clietnIpInfoInRequest.ConnectionIpEndPointsString))
								{
									requestWithClientIpInfo.ClientIpInfo = socketClientIpInfo;
								}
							}
						}
						if (
							request
							is ServiceAdministratorRequest adminRequest
						)
						{
							// !!!
							serviceAdministratorRequest = adminRequest;
							// !!!

							var serviceAdministratorService = BaoXia.Utils.Environment.GetService<ServiceAdministratorService>();
							if (serviceAdministratorService == null)
							{
								return new(
									new JsonResult(
										new Response()
										{
											Error =
												Error.OperationNotImplemented,
											ErrorDescription =
												"服务管理员服务未实现。",
										}
									),
									null
								);
							}
							else if (
								serviceAdministratorService.GetErrorResponseForNeedAdministratorSessionWithRequest(
									adminRequest
								) is
								{ } errorResponseForNeedAdministratorSession
							)
							{
								var responseStatusCode = (int)
									System
										.Net
										.HttpStatusCode
										.OK;
								if (
									Error.UserTokenNotExisted.EqualsIgnoreCase(
										errorResponseForNeedAdministratorSession.Error
									)
									|| Error.UserTokenExpired.EqualsIgnoreCase(
										errorResponseForNeedAdministratorSession.Error
									)
									|| Error.UserTokenSignedInvalid.EqualsIgnoreCase(
										errorResponseForNeedAdministratorSession.Error
									)
								)
								{
									responseStatusCode = (int)
										System
											.Net
											.HttpStatusCode
											.Unauthorized;
								}
								else if (
									Error.UserTokenNeedPermission.EqualsIgnoreCase(
										errorResponseForNeedAdministratorSession.Error
									)
								)
								{
									responseStatusCode = (int)
										System
											.Net
											.HttpStatusCode
											.Forbidden;
								}
								return new(
									new JsonResult(
										errorResponseForNeedAdministratorSession
									)
									{
										StatusCode =
											responseStatusCode,
									},
									null
								);
							}
							else
							{
								// !!!
								serviceAdministratorRequest =
									adminRequest;
								// !!!
							}
						}
					}
				}
			}
			if (serviceAdministratorRequest == null)
			{
				return new(
					new JsonResult(
						new Response()
						{
							Error = Error.InvalidRequest,
							ErrorDescription =
								$"服务管理员方法必须使用至少一个基于“{nameof(ServiceAdministratorRequest)}”的请求。",
						}
					),
					null
				);
			}
		}
		else
		{
			if (requestMethodParamValueList?.Count > 0)
			{
				foreach (var requestMethodParamValue in requestMethodParamValueList)
				{
					if (requestMethodParamValue is BaoXia.Constants.Models.Request request)
					{
						if (request.GetErrorResonseByCheckRequest(RequestPipelineType.BxService) is { } errorResponse)
						{
							return new(new JsonResult(errorResponse), null);
						}
						if (requestMethodParamValue is RequestWithClientIpInfo requestWithClientIpInfo)
						{
							if (apiRequestInfo.ClientIpInfo is { } socketClientIpInfo)
							{
								var clietnIpInfoInRequest =
									requestWithClientIpInfo.ClientIpInfo;
								if (string.IsNullOrEmpty(clietnIpInfoInRequest.ConnectionIpEndPointsString))
								{
									requestWithClientIpInfo.ClientIpInfo = socketClientIpInfo;
								}
							}
						}
					}
				}
			}
		}

		object? controllerMethodResult;
		BrServiceResponseOptionAttribute? responseOption = null;
		try
		{
			controllerMethodResult = controllerMethodMatched.Invoke(
				controllerMatched,
				requestMethodParamValueList?.ToArray()
			);

			responseOption =
				controllerMethodMatched
					.GetCustomAttributes(
						typeof(BrServiceResponseOptionAttribute),
						true
					)
					.FirstOrDefault() as BrServiceResponseOptionAttribute;
		}
		catch (Exception exception)
		{
			var apiNameAttribute =
				controllerMethodMatched
					.GetCustomAttributes(typeof(ApiNameAttribute), true)
					.FirstOrDefault() as ApiNameAttribute;
			////////////////////////////////////////////////
			var apiName = apiNameAttribute?.Name ?? routeAction.RouteInfo.ActionPath;
			var apiLogContentForException =
				apiNameAttribute?.LogContentForException
				?? apiName + "失败，程序异常。";
			// !!! 记录异常日志 !!!
			{
				ExceptionLogFile?.Logs(this, apiLogContentForException, exception);
			}
			////////////////////////////////////////////////
			return new(
				new JsonResult(
					new BaoXia.Constants.Models.Response()
					{
						Error = Error.ProgramError,
						ErrorDescription = "获取请求结果失败，程序异常。",
					}
				),
				responseOption
			);
		}

		if (controllerMethodResult is IActionResult actionResult)
		{
			return new(actionResult, responseOption);
		}
		else if (controllerMethodResult is Task<IActionResult> actionResultAsync)
		{
			var finalActionResult = await actionResultAsync;
			{ }
			return new(finalActionResult, responseOption);
		}
		return new(
			new StatusCodeResult((int)System.Net.HttpStatusCode.NotImplemented),
			null
		);
	}

	protected void AddRequestInfoBytesToProcessed(
		SocketInfo socketInfo,
		//
		byte[] requestInfoBytes,
		ClientConnectionInfo clientConnectionInfo,
		//
		System.Text.Encoding requestTextEncoding,
		//
		ServiceRequestPipeTimeInfo requestPipeTimeInfo,
		DateTime timeOfReceiveData
	)
	{
		if (requestInfoBytes.Length < 1)
		{
			return;
		}

		var serviceConfig = Config;
		if (serviceConfig.RequestStatisticsInLiveSeconds <= 0)
		{
			return;
		}

		// ⏱⏱⏱，【计时埋点】
		requestPipeTimeInfo.SetTime_0_ToAddRequestBytesToNeedProcessQueue(
			timeOfReceiveData
		);
		// ⏱⏱⏱，【计时埋点】
		_requestInfoBytesInfesNeedProcessed.Enqueue(
			new ApiRequestInfoBytesInfo(
				clientConnectionInfo,
				socketInfo,
				requestInfoBytes,
				requestTextEncoding,
				//
				requestPipeTimeInfo,
				timeOfReceiveData
			)
		);

		_ = _tasksToProcessRequestInfes.TryRun(async () =>
		{
			while (_requestInfoBytesInfesNeedProcessed.TryDequeue(out var apiRequestInfoBytesInfo))
			{
				if (
					apiRequestInfoBytesInfo == null
					|| apiRequestInfoBytesInfo.Bytes.Length < 1
				)
				{
					continue;
				}

				var apiRequestInfoTimeOfProcessBegin = DateTime.Now;

				// ⏱⏱⏱，【计时埋点】
				apiRequestInfoBytesInfo.ServiceRequestPipeTimeInfo.SetTime_1_ToDeserializeRequestBytes();
				// ⏱⏱⏱，【计时埋点】
				var dataPacket = DataPacket.CreateDataPacketWithBytes(
					apiRequestInfoBytesInfo.Bytes, apiRequestInfoBytesInfo.TextEncoding,
					apiRequestInfoBytesInfo.ClientConnectionInfo.ApplicationInfo.AesKey
				);
				if (dataPacket == null)
				{
					continue;
				}

				var apiRequestInfo = dataPacket.ToApiRequestInfo<JsonElement>();
				if (apiRequestInfo == null)
				{
					continue;
				}
				// !!!
				apiRequestInfo.ClientIpInfo = apiRequestInfoBytesInfo
					.SocketInfo
					.ClientIpInfo;
				apiRequestInfo.ClientConnectionInfo =
					apiRequestInfoBytesInfo.ClientConnectionInfo;
				apiRequestInfo.ServiceRequestPipeTimeInfo =
					apiRequestInfoBytesInfo.ServiceRequestPipeTimeInfo;
				// !!!

				if (
					ApiRequestIdentity.Client.Heartbeat.EqualsIgnoreCase(
						apiRequestInfo.RequestIdentity
					)
				)
				{
					//DebugInfo.WriteLine(
					//	"心跳包测试", "服务端",
					//	"收到心跳包。");

					apiRequestInfoBytesInfo.SocketInfo.LastTimeOfReceiveHeartbeat = timeOfReceiveData;

					if (
						apiRequestInfo.ServiceRequestPipeTimeInfo
						is ServiceRequestPipeTimeInfo apiRequestPipeTimeInfo
					)
					{
						// ⏱⏱⏱，【类型标记】
						apiRequestPipeTimeInfo.Type =
							RequestPipeType.HeartbeatRequestFromCient;
						// ⏱⏱⏱，【类型标记】
						// !!! 心跳包没有后续处理 !!!
						// !!! 没有真实操作，不记录时间 !!!
						//apiRequestPipeTimeInfo.SetTime_3_ToCreateResponseObject();
						//apiRequestPipeTimeInfo.SetTime_4_ToSerializeResponseObject();
						//apiRequestPipeTimeInfo.SetTime_5_ToAddResponseBytesToNeedSendQueue();
						//apiRequestPipeTimeInfo.SetTime_6_ToSendResponseBytesBegin();
						//apiRequestPipeTimeInfo.SetTime_7_ToSendResponseBytesEnd(DateTime.Now);
					}
				}
				else
				{
					apiRequestInfoBytesInfo.SocketInfo.LastTimeOfReceiveBusinessData = timeOfReceiveData;

					// !!! 心跳包不需要进行额外的处理。 !!!
					await ProcessApiRequestInfoAsync(apiRequestInfo);
					// !!!
				}
			}
		});
	}

	protected async Task<bool> TryToProcessRequestInContextAsync(HttpContext httpContext)
	{
		var webSockets = httpContext.WebSockets;
		if (!webSockets.IsWebSocketRequest)
		{
			return false;
		}

		////////////////////////////////////////////////
		// 1，建立数据连接：
		////////////////////////////////////////////////

		//Console.WriteLine("#连接性能测试#，收到连接请求时间：" + DateTime.Now.ToString("HH:mm:ss fff"));
		////////////////////////////////////////////////
		using var socketToClient = await webSockets.AcceptWebSocketAsync();
		if (socketToClient == null)
		{
			return false;
		}

		if (GetRequestTargetWebSocketServiceTypeInHttpContext(httpContext) is { } websocketServiceType)
		{
			using var scope = BaoXia.Utils.Environment.CreateServiceScope();
			if (scope?.ServiceProvider.GetService(websocketServiceType) is IWebSocketService websocketService)
			{
				////////////////////////////////////////////////
				// 1/2，初始化，并进行WebSocket服务：
				////////////////////////////////////////////////
				{
					websocketService.InitializeWithBrServiceMiddleware(this, httpContext, socketToClient);
				}
				if (!await websocketService.AcceptWebSocketInHttpContextAsync(
					httpContext, socketToClient, WebServiceConfig.WebSocketAuthenticationDeadlineSeconds))
				{
					// 握手已升级，鉴权失败也不能继续进入普通 HTTP 管道。
					return true;
				}

				////////////////////////////////////////////////
				// 2/2，初始化，日常业务处理：
				////////////////////////////////////////////////
				var webSocketServiceSocketCloseInfo = await websocketService.ProcessWebSocketInBrServiceMiddlewareAsync(
					this, httpContext, socketToClient);
				if (webSocketServiceSocketCloseInfo != null)
				{
					await websocketService.DiscountWebSocketInHttpContextAsync(
						httpContext, socketToClient, webSocketServiceSocketCloseInfo);
				}
				return true;
				// !!!
			}
			else
			{
				var pathNotExistingResponse = new Response()
				{
					Error = Error.OperationNotImplemented,
					ErrorDescription = $"制定路径（{httpContext.Request.Path.Value}）对应的服务未实现。"
				};
				await socketToClient.SendObjectAsync(pathNotExistingResponse, CancellationToken.None);
				// !!!
				await socketToClient.CloseAsync(
					WebSocketCloseStatus.NormalClosure, pathNotExistingResponse.ErrorDescription, CancellationToken.None);
				// !!!
				return true;
			}
		}
		else if (httpContext.Request.Path.Value is { } webSocketRequestPath
			&& webSocketRequestPath.Length > 0
			&& (webSocketRequestPath.Length > 1 || webSocketRequestPath[0] != UriSeparators.PathSeparator))
		{
			var pathNotExistingResponse = new Response()
			{
				Error = Error.ObjectNotExisted,
				ErrorDescription = $"要访问的路径（{webSocketRequestPath}）不存在。"
			};
			await socketToClient.SendObjectAsync(pathNotExistingResponse, CancellationToken.None);
			// !!!
			await socketToClient.CloseAsync(
				WebSocketCloseStatus.NormalClosure, pathNotExistingResponse.ErrorDescription, CancellationToken.None);
			// !!!
			return true;
		}

		Interlocked.Increment(ref _totalSocketsToClientCountAccepted);

		// 服务关闭后，不再接受新的连接：
		if (IsServiceShuttingDown)
		{
			// !!!
			await socketToClient.CloseAsync(WebSocketCloseStatus.EndpointUnavailable,
				SocketCloseStatusDescription.ServiceInfo_EndpointUnavailable_ServiceShutdown, CancellationToken.None);
			// !!!
			return true;
		}

		////////////////////////////////////////////////
		//Console.WriteLine("#连接性能测试#，完成连接请求时间：" + DateTime.Now.ToString("HH:mm:ss fff"));

		////////////////////////////////////////////////
		// 2，连接的基础参数：
		////////////////////////////////////////////////

		SocketInfo? socketInfoToClient = null;
		var serviceConfig = Config;
		var applicationRequestReceiveBufferSizeInByte =
			serviceConfig.ApplicationRequestReceiveBufferSizeInByte;
		if (applicationRequestReceiveBufferSizeInByte <= 0)
		{
			applicationRequestReceiveBufferSizeInByte = ReceiveBufferSizeInByteDefault.ReceiveApplicationRequest;
		}
		var clientConnectionOpenTimeoutSeconds = serviceConfig.ClientConnectionOpenTimeoutSeconds;
		if (clientConnectionOpenTimeoutSeconds <= 0)
		{
			clientConnectionOpenTimeoutSeconds = TimeoutSecondsDefault.ClientConnectionOpen;
		}
		var receiveBuffer = new BytesBuffer(applicationRequestReceiveBufferSizeInByte);

		////////////////////////////////////////////////
		// 3，等待应用程序注册：
		////////////////////////////////////////////////

		ClientConnectionInfo? clientConnectionInfo = null;
		ServiceRequestPipeTimeInfo clientOpenConnectApiRequestPipeTimeInfo = new(RequestPipeType.ClientConnectionOpen);
		WebSocketCloseStatus? webSocketCloseStatusByService = null;
		string? webSocketCloseStatusDescriptionByService = null;
		WebSocketCloseStatus? webSocketCloseStatusByClient = null;
		string? webSocketCloseStatusDescriptionByClient = null;
		CancellationTokenSource applicationRegisterCancelTokenSource = new();
		var applicationRegisterCancelToken = applicationRegisterCancelTokenSource.Token;
		try
		{
			applicationRegisterCancelTokenSource.CancelAfter((int)(clientConnectionOpenTimeoutSeconds * 1000));
			ApiRequestInfo<JsonElement>? clientOpenConnectRequest = null;
			try
			{
				clientOpenConnectRequest = await socketToClient.ReceiveApiRequestInfoAsync<JsonElement>(
					receiveBuffer, Config.ClientRequestTextEncoding, string.Empty, RequestPipeType.ClientConnectionOpen,
					applicationRegisterCancelToken);
			}
			catch
			{
				clientOpenConnectRequest = null;
			}

			if (clientOpenConnectRequest == null)
			{
				// !!! 应用程序信息无效，使用默认的管道信息记录 !!!
				{
					// ⏱⏱⏱，【信息记录】
					AddServiceRequestPipeTimeInfo(clientOpenConnectApiRequestPipeTimeInfo, RequestPipeType.ClientConnectionOpen);
					// ⏱⏱⏱，【信息记录】
				}
				webSocketCloseStatusByService = WebSocketCloseStatus.PolicyViolation;
				webSocketCloseStatusDescriptionByService
					= SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_RequestParseWithJsonFailed;
				// !!!
				throw new BadHttpRequestException(webSocketCloseStatusDescriptionByService);
			}
			else
			{
				// !!!
				clientOpenConnectApiRequestPipeTimeInfo = clientOpenConnectRequest.ServiceRequestPipeTimeInfo
					?? clientOpenConnectApiRequestPipeTimeInfo;
				// !!!
				// ⏱⏱⏱，【信息记录】
				AddServiceRequestPipeTimeInfo(clientOpenConnectApiRequestPipeTimeInfo, RequestPipeType.ClientConnectionOpen);
				// ⏱⏱⏱，【信息记录】

				if (clientOpenConnectRequest.WebSocketReceiveResult != null)
				{
					var webSocketReceiveResult = clientOpenConnectRequest.WebSocketReceiveResult;
					// !!!
					webSocketCloseStatusByClient = webSocketReceiveResult.CloseStatus;
					webSocketCloseStatusDescriptionByClient = webSocketReceiveResult.CloseStatusDescription;
					// !!!
				}
			}

			var applicationIdentity = clientOpenConnectRequest.ApplicationIdentity;
			if (string.IsNullOrEmpty(applicationIdentity))
			{
				// !!! 应用程序信息无效 !!!
				// ⏱⏱⏱，【状态标记】
				clientOpenConnectApiRequestPipeTimeInfo.State =
					RequestPipeState.Failed;
				// ⏱⏱⏱，【状态标记】

				webSocketCloseStatusByService =
					WebSocketCloseStatus.PolicyViolation;
				webSocketCloseStatusDescriptionByService =
					SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_NeedApplicationIdentity;
				// !!!
				throw new BadHttpRequestException(
					webSocketCloseStatusDescriptionByService
				);
			}
			var requestIdentity = clientOpenConnectRequest.RequestIdentity;
			if (string.IsNullOrEmpty(requestIdentity))
			{
				// !!! 应用程序信息无效 !!!
				// ⏱⏱⏱，【状态标记】
				clientOpenConnectApiRequestPipeTimeInfo.State = RequestPipeState.Failed;
				// ⏱⏱⏱，【状态标记】

				webSocketCloseStatusByService = WebSocketCloseStatus.PolicyViolation;
				webSocketCloseStatusDescriptionByService = SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_NeedRequestIdentity;
				// !!!
				throw new BadHttpRequestException(webSocketCloseStatusDescriptionByService);
			}

			var methodName = clientOpenConnectRequest.MethodName;
			if (string.IsNullOrEmpty(methodName))
			{
				// !!! 应用程序信息无效 !!!
				// ⏱⏱⏱，【状态标记】
				clientOpenConnectApiRequestPipeTimeInfo.State = RequestPipeState.Failed;
				// ⏱⏱⏱，【状态标记】

				webSocketCloseStatusByService = WebSocketCloseStatus.PolicyViolation;
				webSocketCloseStatusDescriptionByService = SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_NeedMethodName;
				// !!!
				throw new BadHttpRequestException(webSocketCloseStatusDescriptionByService);
			}

			// !!! 服务中的方法名称（methodName），对应HTTP请求中的请求路径 !!!
			using var routeAction = GetRouteActionWithRequestUriPath(methodName);
			if (routeAction == null)
			{
				// !!! 应用程序信息无效 !!!
				// ⏱⏱⏱，【状态标记】
				clientOpenConnectApiRequestPipeTimeInfo.State =
					RequestPipeState.Failed;
				// ⏱⏱⏱，【状态标记】

				webSocketCloseStatusByService =
					WebSocketCloseStatus.PolicyViolation;
				webSocketCloseStatusDescriptionByService =
					SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_MethodNotFound;
				// !!!
				throw new BadHttpRequestException(webSocketCloseStatusDescriptionByService);
			}

			// 强制使用内置的“ServiceSessionController.OpenConnect”：
			using var sessionController = new BrClientConnectionInfoController(this);
			{
				var actionResult = await this.InvokeApiWithApiRequestInfoAsync(
					clientOpenConnectRequest,
					null,
					socketToClient,
					applicationRegisterCancelToken,
					routeAction,
					sessionController);
				if (actionResult?.ResultInObject is BrClientConnectionInfoController.OpenConnectResponse openConnectionResponse)
				{
					clientConnectionInfo = BrClientConnectionInfoController.GetClientConnectionInfoWithId(
						openConnectionResponse.ConnectionId);
				}
			}
		}
		catch (Exception exception)
		{
			if (webSocketCloseStatusByService == null)
			{
				if (exception is OperationCanceledException operationCanceledException
					&& operationCanceledException.CancellationToken == applicationRegisterCancelToken)
				{
					// !!! 应用程序信息注册超时 !!!
					webSocketCloseStatusByService = WebSocketCloseStatus.PolicyViolation;
					webSocketCloseStatusDescriptionByService
						= SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_ConnectTimeout;
					// !!!
				}
				else
				{
					// !!! 应用程序信息注册失败，程序错误 !!!
					webSocketCloseStatusByService = WebSocketCloseStatus.InternalServerError;
					webSocketCloseStatusDescriptionByService
						= SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_ConnectFailedByProgramError;
					// !!!
				}
			}
		}
		finally
		{
			if (clientConnectionInfo == null)
			{
				// !!! 应用程序信息注册失败 !!!
				webSocketCloseStatusByService = WebSocketCloseStatus.PolicyViolation;
				webSocketCloseStatusDescriptionByService = SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_ConnectFailed;
				// !!!
			}

			////////////////////////////////////////////////
			// 处理异常导致的连接关闭信息：
			////////////////////////////////////////////////
			if (webSocketCloseStatusByClient != null)
			{
				await socketToClient.TryToCloseAsync(
					webSocketCloseStatusByClient.Value, webSocketCloseStatusDescriptionByClient, CancellationTokenSource.Token);
			}
			else if (webSocketCloseStatusByService != null)
			{
				await socketToClient.TryToCloseAsync(webSocketCloseStatusByService.Value,
					webSocketCloseStatusDescriptionByService, CancellationTokenSource.Token);
			}
		}
		////////////////////////////////////////////////
		////////////////////////////////////////////////
		////////////////////////////////////////////////
		if (clientConnectionInfo == null || socketToClient.IsConnectionEnable() != true)
		{
			// ⏱⏱⏱，【状态标记】
			clientOpenConnectApiRequestPipeTimeInfo.State = RequestPipeState.Failed;
			// ⏱⏱⏱，【状态标记】
			// !!! 连接已关闭，本次请求按“已处理”返回结果 !!!
			return true;
		}
		else
		{
			var clientIpInfo = httpContext.Request.GetClientIpInfo();
			// !!! ⚠️ 尝试加入连接 ⚠️!!!
			if (clientConnectionInfo.TryAddSocket(socketToClient, clientIpInfo, out socketInfoToClient) != true)
			{
				////////////////////////////////////////////////
				await socketToClient.TryToCloseAsync(WebSocketCloseStatus.PolicyViolation,
					SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_TooManyConnections,
					CancellationTokenSource.Token);
				////////////////////////////////////////////////
				// ⏱⏱⏱，【状态标记】
				clientOpenConnectApiRequestPipeTimeInfo.State = RequestPipeState.Failed;
				// ⏱⏱⏱，【状态标记】
				return true;
			}
		}

		////////////////////////////////////////////////
		// 4，开始服务：
		////////////////////////////////////////////////
		WebSocketReceiveResult? socketToClientReceiveResult = null;
		CancellationToken socketReceiveDataCancellationToken = clientConnectionInfo
			.CancellationTokenSource
			.Token;
		try
		{
			var clientRequestTextEncoding = Config.ClientRequestTextEncoding;

			DebugInfo.WriteLine(
				"关机测试",
				"服务端",
				$"TryToProcessRequestInContextAsync，开始接收客户端请求的循环。"
			);

			await foreach (
				var apiRequestInfoBytesResult in socketInfoToClient.Socket.ReceiveByteArraysAsync(
					receiveBuffer,
					socketReceiveDataCancellationToken
				)
			)
			{
				// !!!
				var timeOfReceiveData = DateTime.Now;
				// !!!
				if (apiRequestInfoBytesResult == null)
				{
					continue;
				}

				// ⏱⏱⏱，【计时器创建】
				var apiRequestPipeTimeInfo = new ServiceRequestPipeTimeInfo(
					RequestPipeType.ApiInvokeRequest
				);
				// ⏱⏱⏱，【计时器创建】
				// ⏱⏱⏱，【信息记录】
				AddServiceRequestPipeTimeInfo(apiRequestPipeTimeInfo);
				// ⏱⏱⏱，【信息记录】

				if (IsServiceShuttingDown)
				{
					DebugInfo.WriteLine(
						"关机测试",
						"服务端",
						$"TryToProcessRequestInContextAsync，关机后，收到请求：{apiRequestInfoBytesResult.BytesReceived?.Length}。"
					);
				}

				// !!!
				socketToClientReceiveResult = apiRequestInfoBytesResult.Result;
				// !!!

				if (
					apiRequestInfoBytesResult.BytesReceived
					is byte[] apiRequestInfoBytes
				)
				{
					// !!!
					Interlocked.Increment(ref _totalRequestsCountReceived);
					// !!!
					AddRequestInfoBytesToProcessed(
						socketInfoToClient,
						//
						apiRequestInfoBytes,
						clientConnectionInfo,
						clientRequestTextEncoding,
						//
						apiRequestPipeTimeInfo,
						timeOfReceiveData
					);
					// !!!
				}

				////////////////////////////////////////////////
				// 关闭Socket的操作，在超时范围内，应当由客户端发起完成。
				// 关机后不再接收新的请求信息
				if (socketToClientReceiveResult?.CloseStatus != null)
				{
					// !!! ⚠
					break;
					// !!! ⚠
				}
				////////////////////////////////////////////////
			} // await foreach end

			DebugInfo.WriteLine(
				"关机测试",
				"服务端",
				$"TryToProcessRequestInContextAsync，结束接收客户端请求的循环。"
			);
		}
		catch (Exception exception)
		{
			if (
				exception
					is not OperationCanceledException operationCanceledException
				|| operationCanceledException.CancellationToken
					!= applicationRegisterCancelToken
			)
			{
				ExceptionLogFile?.Logs(
					this,
					"处理服务请求失败，程序异常。",
					exception
				);
			}
		}
		finally
		{
			try
			{
				////////////////////////////////////////////////
				// 正常情况下，关闭连接都是由客户端完成的，
				// 因此非客户端关闭的连接，都是异常情况。
				////////////////////////////////////////////////

				WebSocketCloseStatus closeStatus;
				string? closeStatusDescription;
				if (socketToClientReceiveResult?.CloseStatus != null)
				{
					closeStatus = socketToClientReceiveResult.CloseStatus.Value;
					closeStatusDescription =
						socketToClientReceiveResult.CloseStatusDescription;
				}
				else
				{
					closeStatus = WebSocketCloseStatus.EndpointUnavailable;
					closeStatusDescription =
						SocketCloseStatusDescription.ServiceInfo_EndpointUnavailable_UnexpectedCloseOperation;
				}
				// !!!
				await socketToClient.TryToCloseAsync(
					closeStatus,
					closeStatusDescription,
					clientConnectionInfo.CancellationTokenSource.Token
				);
				//
				Interlocked.Increment(ref _totalSocketsToClientCountClosed);
				// !!!

				DebugInfo.WriteLine(
					"关机测试",
					"服务端",
					$"TryToProcessRequestInContextAsync，关闭Socket。"
				);
			}
			catch (Exception exception)
			{
				ExceptionLogFile?.Logs(
					this,
					"关闭Socket连接失败，程序异常。",
					exception
				);
			}
			// !!! ⚠️
			clientConnectionInfo.RemoveSocket(socketToClient);
			// !!! ⚠️

			BrClientConnectionInfoController.TryRemoveClientConnectionInfo(
				clientConnectionInfo
			);
		}

		return true;
	}

	protected async Task<ApiResponseInfoAbstract?> InvokeApiWithApiRequestInfoAsync(
		ApiRequestInfo<JsonElement> apiRequestInfo,
		ClientConnectionInfo? clientConnectionInfo,
		WebSocket? socketToClientSpecified,
		CancellationToken cancellationToken,
		RouteAction? routeActionSpecified = null,
		object? controllerSpecified = null)
	{
		var requestIdentity = apiRequestInfo.RequestIdentity;
		if (string.IsNullOrEmpty(requestIdentity))
		{
			return null;
		}

		ApiResponseInfoAbstract? apiResponseInfo = null;
		var apiResponseEncryptionMethod = DataPackageEncryptionMethod.None;
		string apiResponseAesKey =
			clientConnectionInfo?.ApplicationInfo.AesKey ?? string.Empty;
		try
		{
			////////////////////////////////////////////////
			// 1/2，内置服务实现：
			////////////////////////////////////////////////
			// ************************************************
			// 内置服务实现，1/3，心跳响应。
			// ************************************************
			if (ApiRequestIdentity.Client.Heartbeat.EqualsIgnoreCase(apiRequestInfo.RequestIdentity))
			{
				if (apiRequestInfo.ServiceRequestPipeTimeInfo is ServiceRequestPipeTimeInfo serviceRequestPipeTimeInfo)
				{
					// ⏱⏱⏱，【计时埋点】
					serviceRequestPipeTimeInfo.SetTime_2_ToRouteRequest();
					// ⏱⏱⏱，【计时埋点】
					{
						// “心跳包”的“请求路由操作”。
					}
					// ⏱⏱⏱，【计时埋点】
					serviceRequestPipeTimeInfo.SetTime_3_ToCreateResponseObject();
					// ⏱⏱⏱，【计时埋点】
				}

				apiResponseInfo = new ApiResponseInfo<object>(requestIdentity)
				{
					StatusCode = (int)System.Net.HttpStatusCode.OK,
				};
			}
			// ************************************************
			// 内置服务实现，2/3，【设置“客户端”关闭连接】命令。
			// ************************************************
			else if (ApiRequestIdentity.Client.CloseConnection.EqualsIgnoreCase(apiRequestInfo.RequestIdentity))
			{
				if (apiRequestInfo.ServiceRequestPipeTimeInfo is ServiceRequestPipeTimeInfo serviceRequestPipeTimeInfo)
				{
					// ⏱⏱⏱，【计时埋点】
					serviceRequestPipeTimeInfo.SetTime_2_ToRouteRequest();
					// ⏱⏱⏱，【计时埋点】
					{
						// “心跳包”的“请求路由操作”。
					}
					// ⏱⏱⏱，【计时埋点】
					serviceRequestPipeTimeInfo.SetTime_3_ToCreateResponseObject();
					// ⏱⏱⏱，【计时埋点】
				}

				var clientConnectionInfoController =
					new BrClientConnectionInfoController(this);
				// !!!
				var closeConnectResponse = clientConnectionInfoController.CloseConnection(
					new BrClientConnectionInfoController.CloseConnectRequest()
					{
						ConnectionId = clientConnectionInfo?.Id ?? 0,
					});
				// !!!

				apiResponseInfo = closeConnectResponse?.ToApiResponseInfo(requestIdentity, out _);
			}
			// ************************************************
			// 内置服务实现，3/3，【设置“服务端”关机】命令。
			// ************************************************
			else if (ApiRequestIdentity.Service.Shutdown.EqualsIgnoreCase(apiRequestInfo.RequestIdentity))
			{
				// !!!
				DebugInfo.WriteLine("关机测试", "服务端", $"收到”关机Api“的调用。");
				// !!!

				if (apiRequestInfo.ServiceRequestPipeTimeInfo is ServiceRequestPipeTimeInfo serviceRequestPipeTimeInfo)
				{
					// ⏱⏱⏱，【计时埋点】
					serviceRequestPipeTimeInfo.SetTime_2_ToRouteRequest();
					// ⏱⏱⏱，【计时埋点】
					{
						// “心跳包”的“请求路由操作”。
					}
					// ⏱⏱⏱，【计时埋点】
					serviceRequestPipeTimeInfo.SetTime_3_ToCreateResponseObject();
					// ⏱⏱⏱，【计时埋点】
				}

				// !!!
				_ = ShutdownAsync();
				// !!!
				apiResponseInfo = new ApiResponseInfo<object>(requestIdentity)
				{
					StatusCode = (int)System.Net.HttpStatusCode.OK,
				};
			}
			////////////////////////////////////////////////
			// 2/2，业务服务实现：
			////////////////////////////////////////////////
			else
			{
				RouteAction? routeAction;
				// ⏱⏱⏱，【计时埋点】
				apiRequestInfo.ServiceRequestPipeTimeInfo?.SetTime_2_ToRouteRequest();
				// ⏱⏱⏱，【计时埋点】
				if (routeActionSpecified != null)
				{
					routeAction = routeActionSpecified;
				}
				else
				{
					routeAction = GetRouteActionWithRequestUriPath(
						apiRequestInfo.MethodName
					);
				}
				// ⏱⏱⏱，【计时埋点】
				apiRequestInfo.ServiceRequestPipeTimeInfo?.SetTime_3_ToCreateResponseObject();
				// ⏱⏱⏱，【计时埋点】

				////////////////////////////////////////////////
				// 1/2，获取“ApiResponseInfo”：
				////////////////////////////////////////////////
				// IActionResult? actionResult = null;
				RouteAction? nextRouteAction = null;
				do
				{
					var routeActionInvokeResultInfo = await GetActionResultWithRouteActionAsync(
						routeAction, apiRequestInfo, controllerSpecified);
					var routeActionInvokeResult = routeActionInvokeResultInfo.Result;
					if (routeActionInvokeResult != null)
					{
						if (routeActionInvokeResultInfo.ResponseOption is { } responseOption)
						{
							// !!!
							apiResponseEncryptionMethod =
								responseOption.EncryptionMethod;
							// !!!
						}

						apiResponseInfo = routeActionInvokeResult.ToApiResponseInfo(requestIdentity, out var actionRedirectUrl);
						if (apiResponseInfo == null && !string.IsNullOrEmpty(actionRedirectUrl))
						{
							nextRouteAction = GetRouteActionWithRequestUriPath(actionRedirectUrl);
							if (nextRouteAction?.RouteInfo.Equals(routeAction?.RouteInfo) == true)
							{
								// actionResult = redirectResult;
								apiResponseInfo = new ApiResponseInfo<string>(requestIdentity)
								{
									StatusCode = (int)System.Net.HttpStatusCode.LoopDetected
								};
							}
						}
					}
				} while (nextRouteAction != null);

				apiResponseInfo ??= new ApiResponseInfo<object>(requestIdentity)
				{
					StatusCode = (int)System.Net.HttpStatusCode.NotFound,
				};
			}
		}
		catch (Exception exception)
		{
			ExceptionLogFile?.Logs(this, "调用Api失败，程序异常。", exception);
			//

			if (apiRequestInfo.ServiceRequestPipeTimeInfo is ServiceRequestPipeTimeInfo serviceRequestPipeTimeInfo)
			{
				// ⏱⏱⏱，【状态标记】
				serviceRequestPipeTimeInfo.State = RequestPipeState.Exception;
				// ⏱⏱⏱，【状态标记】
			}

			//
			apiResponseInfo = new ApiResponseInfo<object>(requestIdentity)
			{
				StatusCode = (int?)System.Net.HttpStatusCode.InternalServerError,
			};
		}

		////////////////////////////////////////////////
		// !!! 如果发生意外，没产生“apiResponseInfo”，则创建一个默认的： !!!
		apiResponseInfo ??= new ApiResponseInfo<object>(requestIdentity)
		{
			StatusCode = (int)System.Net.HttpStatusCode.NotFound,
		};
		////////////////////////////////////////////////

		////////////////////////////////////////////////
		// ⏱⏱⏱，【信息传递】
		apiResponseInfo.ServiceRequestPipeTimeInfo = apiRequestInfo.ServiceRequestPipeTimeInfo;
		// ⏱⏱⏱，【信息传递】
		////////////////////////////////////////////////

		////////////////////////////////////////////////
		// 2/2，发送“ApiResponseInfo”：
		////////////////////////////////////////////////

		if (socketToClientSpecified != null)
		{
			var serviceConfig = Config;
			await socketToClientSpecified.SendApiResponseInfoAsync(
				apiResponseInfo,
				//
				serviceConfig.ServiceResponseTextEncoding,
				apiResponseEncryptionMethod,
				apiResponseAesKey,
				serviceConfig.ServiceResponseDataPacketSizeToCompression,
				serviceConfig.ServiceResponseDataPacketCompressionMethodName,
				//
				cancellationToken
			);
			// !!!
			Interlocked.Increment(ref _totalResponsesCountSend);
			// !!!
		}
		else
		{
			// !!! ⚠
			clientConnectionInfo?.AddApiResponseInfoToSendToClient(
				apiResponseInfo, this, Config, apiResponseEncryptionMethod, apiResponseAesKey);
			// !!! ⚠
		}
		return apiResponseInfo;
	}

	protected async Task<bool> ProcessApiRequestInfoAsync(ApiRequestInfo<JsonElement>? apiRequestInfo)
	{
		if (apiRequestInfo?.IsValid != true)
		{
			return false;
		}
		var clientConnectionInfo = apiRequestInfo.ClientConnectionInfo;
		if (clientConnectionInfo == null)
		{
			return false;
		}

		var processResult = await DidProcessApiRequestInfoAsync(
			apiRequestInfo,
			clientConnectionInfo,
			clientConnectionInfo.CancellationTokenSource.Token
		);
		{ }
		return processResult;
	}

	public int BroadcastResponseToClient<MethodResultType>(string? requestIdentity, MethodResultType? response,
		DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.None,
		int httpStatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status200OK)
	{
		if (string.IsNullOrEmpty(requestIdentity))
		{
			return 0;
		}

		var apiResponseInfo = new ApiResponseInfo<MethodResultType>()
		{
			RequestIdentity = requestIdentity,
			StatusCode = httpStatusCode,
			Result = response,
		};

		var clientConnectionInfes = BrClientConnectionInfoController.ClientConnectionInfes;
		if (clientConnectionInfes.Count < 1)
		{
			return 0;
		}
		var clientConnectionInfesCountApiResponseSent = 0;
		foreach (var clientConnectionInfo in clientConnectionInfes)
		{
			clientConnectionInfo.AddApiResponseInfoToSendToClient(
				apiResponseInfo,
				this,
				Config,
				encryptionMethod,
				clientConnectionInfo.ApplicationInfo.AesKey
			);
			// !!!
			clientConnectionInfesCountApiResponseSent++;
			// !!!
		}
		return clientConnectionInfesCountApiResponseSent;
	}

	public void SetApplicationNormalExit()
	{
		DidSendServiceLifeCycleEventToIServiceLifeCycleEvents(
			ServiceLifeCycleEvent.ApplicationNormalExit,
			CancellationToken.None
		);
	}

	#endregion


	////////////////////////////////////////////////
	// @事件节点
	////////////////////////////////////////////////s

	#region 事件节点

	protected void DidSendServiceLifeCycleEventToIServiceLifeCycleEvents(
		ServiceLifeCycleEvent serviceLifeCycleEvent, CancellationToken cancellationToken)
	{
		string logPrefix;
		string eventOperationName;
		switch (serviceLifeCycleEvent)
		{
			default:
			case ServiceLifeCycleEvent.Unknown:
				{
					return;
				}
			case ServiceLifeCycleEvent.Booting:
				{
					logPrefix = "服务启动中";
					eventOperationName = "加载";
				}
				break;
			case ServiceLifeCycleEvent.Started:
				{
					logPrefix = "服务已启动";
					eventOperationName = "执行";
				}
				break;
			case ServiceLifeCycleEvent.Shutdown:
				{
					logPrefix = "服务关机中";
					eventOperationName = "关闭";
				}
				break;
			case ServiceLifeCycleEvent.ApplicationNormalExit:
				{
					logPrefix = "服务应用程序正常退出";
					eventOperationName = "正常退出";
				}
				break;
		}

		var sendBeginTime = DateTime.Now;

		// 加载“IServiceLifeCycleEvents”：
		var iServiceLifeCycleEventsType = typeof(IServiceLifeCycleEvents);
		var serviceLifeCycleEventsTypes = AssemblyUtil.GetAllTypesWithAnyBaseClassSpecifiedInApplicationAssembly(
			null, null, iServiceLifeCycleEventsType);
		serviceLifeCycleEventsTypes.RemoveIf(type =>
		{
			// 移除“IServiceLifeCycleEvents”自身：
			if (type == iServiceLifeCycleEventsType
			// 移除抽象类：
			|| type.IsAbstract)
			{
				return true;
			}
			return false;
		});

		var serviceLifeCycleEventsTypesCount = serviceLifeCycleEventsTypes.Count;
		BrServiceLogFile?.Logs(this, $"{logPrefix}，生命周期事件服务，已发现：{serviceLifeCycleEventsTypesCount} 个。", null);
		if (serviceLifeCycleEventsTypes != null && serviceLifeCycleEventsTypesCount > 0)
		{
			using var scope = BaoXia.Utils.Environment.CreateServiceScope();
			if (scope != null)
			{
				// 1/3，获得所有服务生命周期事件实例。
				var lifeCycleEventsInstances = new List<IServiceLifeCycleEvents>();
				foreach (
					var serviceLifeCycleEventsType in serviceLifeCycleEventsTypes
				)
				{
					var serviceInstance = scope.ServiceProvider.GetService(
						serviceLifeCycleEventsType
					);
					if (
						serviceInstance
						is IServiceLifeCycleEvents instanceOfIServiceLifeCycleEvents
					)
					{
						if (
							!serviceInstance.Equals(
								iServiceLifeCycleEventsType
							)
						)
						{
							// !!!
							lifeCycleEventsInstances.Add(
								instanceOfIServiceLifeCycleEvents
							);
							// !!!
						}
					}
				}

				// 2/2，按顺序启动加载服务实例：
				var tasksOfAllLifeCycleEventsInstancesBooting = new List<Task>();
				for (
					var orderNumberOfCurrentServiceInBooting = 0;
					;
					orderNumberOfCurrentServiceInBooting++
				)
				{
					var tasksOfCurrentLifeCycleEventsInstancesBooting =
						new List<Task>();
					for (
						var lifeCycleEventsInstanceIndex = 0;
						lifeCycleEventsInstanceIndex
							< lifeCycleEventsInstances.Count;
						lifeCycleEventsInstanceIndex++
					)
					{
						var lifeCycleEventsInstance =
							lifeCycleEventsInstances[
								lifeCycleEventsInstanceIndex
							];
						if (
							lifeCycleEventsInstance.ServiceStartupSequenceNumber
							<= orderNumberOfCurrentServiceInBooting
						)
						{
							////////////////////////////////////////////////
							// !!!
							var lifeCycleEventsInstanceName =
								lifeCycleEventsInstance
									.GetType()
									.Name;
							var taskToBootInstance = Task.Run(
								async () =>
								{
									BrServiceLogFile?.Logs(
										this,
										$"{logPrefix}，生命周期事件服务“{lifeCycleEventsInstanceName}”，开始{eventOperationName}。",
										null
									);
									var serviceBootingBeginTime =
										DateTime.Now;

									////////////////////////////////////////////////
									////////////////////////////////////////////////
									////////////////////////////////////////////////
									// !!!
									switch (serviceLifeCycleEvent)
									{
										default:
										case ServiceLifeCycleEvent.Unknown:
											break;
										case ServiceLifeCycleEvent.Booting:
											{
												var lastBootingProgress =
													0.0;
												await lifeCycleEventsInstance.DidServiceBootingAsync(
													(
														bootingProgress
													) =>
													{
														if (
															bootingProgress
																== 1.0
															|| (
																bootingProgress
																- lastBootingProgress
															)
																>= 0.1
														)
														{
															lastBootingProgress =
																bootingProgress;
															BrServiceLogFile?.Logs(
																this,
																$"{logPrefix}，生命周期事件服务“{lifeCycleEventsInstanceName}”，已{eventOperationName}：{100.0 * bootingProgress:F2} %。",
																null
															);
														}
													},
													cancellationToken
												);
											}
											break;
										case ServiceLifeCycleEvent.Started:
											{
												await lifeCycleEventsInstance.DidServiceStartedAsync(cancellationToken
												);
											}
											break;
										case ServiceLifeCycleEvent.Shutdown:
											{
												var lastShutdownProgress = 0.0;
												await lifeCycleEventsInstance.DidServiceShutdownAsync((shutdownProgress) =>
												{
													if (
														shutdownProgress
															== 1.0
														|| (
															shutdownProgress
															- lastShutdownProgress
														)
															>= 0.1
													)
													{
														lastShutdownProgress =
															shutdownProgress;
														BrServiceLogFile?.Logs(
															this,
															$"{logPrefix}，生命周期事件服务“{lifeCycleEventsInstanceName}”，已{eventOperationName}：{100.0 * shutdownProgress:F2} %。",
															null
														);
													}
												}, cancellationToken);
											}
											break;
										case ServiceLifeCycleEvent.ApplicationNormalExit:
											{
												// !!!
												lifeCycleEventsInstance.DidApplicationNormalExit(
													cancellationToken
												);
												// !!!
											}
											break;
									}

									// !!!
									////////////////////////////////////////////////
									////////////////////////////////////////////////
									////////////////////////////////////////////////

									var serviceBootingEndTime =
										DateTime.Now;
									var serviceBootingDuration =
										(
											serviceBootingEndTime
											- serviceBootingBeginTime
										);
									var serviceBootingEndLogInfo =
										$"{logPrefix}，生命周期事件服务“{lifeCycleEventsInstanceName}”，{eventOperationName}完成，耗时：{serviceBootingDuration.TotalSeconds:F3}秒。";
									BrServiceLogFile?.Logs(
										this,
										serviceBootingEndLogInfo,
										null
									);
								},
								cancellationToken
							);
							// !!!
							////////////////////////////////////////////////

							if (
								lifeCycleEventsInstance.ServiceStartupSequenceNumber
								>= 0
							)
							{
								tasksOfCurrentLifeCycleEventsInstancesBooting.Add(
									taskToBootInstance
								);
								tasksOfAllLifeCycleEventsInstancesBooting.Add(
									taskToBootInstance
								);
							}
							// !!!
							lifeCycleEventsInstances.RemoveAt(
								lifeCycleEventsInstanceIndex
							);
							lifeCycleEventsInstanceIndex--;
							// !!!
						}
					}
					if (orderNumberOfCurrentServiceInBooting == 0)
					{
						continue;
					}
					else
					{
						Task.WaitAll(
							[
								.. tasksOfCurrentLifeCycleEventsInstancesBooting,
							],
							cancellationToken
						);
					}

					if (lifeCycleEventsInstances.Count < 1)
					{
						break;
					}
				}
				// !!!
				Task.WaitAll(
					[.. tasksOfAllLifeCycleEventsInstancesBooting],
					cancellationToken
				);
				// !!!
			}
		}

		var sendDuration = (DateTime.Now - sendBeginTime);

		BrServiceLogFile?.Logs(
			this,
			$"{logPrefix}，生命周期事件服务，共 {serviceLifeCycleEventsTypesCount} 个，全部{eventOperationName}完成，耗时： {sendDuration.TotalSeconds:F3} 秒。",
			null
		);
	}

	/// <summary>
	/// 服务启动中。
	/// </summary>
	/// <param name="cancellationToken">服务启动的取消令牌。</param>
	protected void DidServiceBooting(CancellationToken cancellationToken)
	{
		BrServiceLogFile?.Logs(this, "服务状态变化，服务启动中。", null);

		////////////////////////////////////////////////
		// 1/2，执行服务生命周期事件：
		////////////////////////////////////////////////
		{
			DidSendServiceLifeCycleEventToIServiceLifeCycleEvents(
				ServiceLifeCycleEvent.Booting,
				cancellationToken
			);
		}

		////////////////////////////////////////////////
		// 2/2，调用事件回调：
		////////////////////////////////////////////////
		// !!!
		ToServiceBooting?.Invoke(
			(bootingProgressCaption, bootingProgress) =>
			{
				if (string.IsNullOrEmpty(bootingProgressCaption))
				{
					bootingProgressCaption =
						$"启动中 {(100.0 * bootingProgress):F2}";
				}
				BrServiceLogFile?.Logs(
					this,
					"服务状态变化，服务启动中：" + bootingProgressCaption,
					null
				);
			},
			cancellationToken
		);
		// !!!

		BrServiceLogFile?.Logs(this, "服务状态变化，服务启动完成。", null);
	}

	/// <summary>
	/// 服务已开始。
	/// </summary>
	/// <param name="cancellationToken">服务开始的取消令牌。</param>
	protected async void DidServiceStarted(CancellationToken cancellationToken)
	{
		BrServiceLogFile?.Logs(this, "服务状态变化，服务启动完毕。", null);

		////////////////////////////////////////////////
		// 1/3，执行服务生命周期事件：
		////////////////////////////////////////////////
		{
			DidSendServiceLifeCycleEventToIServiceLifeCycleEvents(
				ServiceLifeCycleEvent.Started,
				cancellationToken
			);
		}

		////////////////////////////////////////////////
		// 2/3，调用事件回调：
		////////////////////////////////////////////////
		// !!!
		ToServiceStarted?.Invoke(cancellationToken);
		// !!!

		////////////////////////////////////////////////
		// !!! 3/3，开启服务发现 !!!
		////////////////////////////////////////////////
		// !!!
		await _serviceDiscoverer.StartAsync(BrServiceLogFile, ExceptionLogFile, () => Config.ServiceDiscovererConfig, false);
		// !!!
	}

	/// <summary>
	/// 服务已关机。
	/// </summary>
	/// <param name="cancellationToken">服务关机的取消令牌。</param>
	protected void DidServiceShutdown(CancellationToken cancellationToken)
	{
		BrServiceLogFile?.Logs(this, "服务状态变化，服务已关机。", null);

		////////////////////////////////////////////////
		// !!! 1/3，关闭服务发现 !!!
		////////////////////////////////////////////////
		{
			_serviceDiscoverer.Stop();
		}

		////////////////////////////////////////////////
		// 2/3，执行服务生命周期事件：
		////////////////////////////////////////////////
		{
			DidSendServiceLifeCycleEventToIServiceLifeCycleEvents(
				ServiceLifeCycleEvent.Shutdown,
				cancellationToken
			);
		}

		////////////////////////////////////////////////
		// 3/3，调用事件回调：
		////////////////////////////////////////////////
		// !!!
		ToServiceShutdown?.Invoke(cancellationToken);
		// !!!
	}

	protected List<Type>? DidCreateControllerTypes()
	{
		var allTypes = new List<Type>();
		{
			var allTypesInCurrentAssembly = Assembly.GetEntryAssembly()?.GetTypes();
			if (allTypesInCurrentAssembly?.Length > 0)
			{
				allTypes.AddRange(allTypesInCurrentAssembly);
			}
			var allTypesInExecutingAssembly = Assembly.GetExecutingAssembly()?.GetTypes();
			if (allTypesInExecutingAssembly?.Length > 0)
			{
				allTypes.AddRange(allTypesInExecutingAssembly);
			}
		}
		if (allTypes.IsEmpty())
		{
			return null;
		}

		var controllerTypes = new List<Type>();
		var mvcContollerType = typeof(Microsoft.AspNetCore.Mvc.Controller);
		foreach (var type in allTypes)
		{
			////////////////////////////////////////////////
			// 忽略抽象类：
			////////////////////////////////////////////////
			if (type.IsAbstract)
			{
				continue;
			}

			var isTypeController = false;
			var isTypeInheritController = mvcContollerType.IsAssignableFrom(type);
			if (isTypeInheritController)
			{
				isTypeController = true;
			}
			else
			{
				var isTypeSuffixWithController = type.Name.EndsWith(
					NamingConventions.ControllerNameSuffix, StringComparison.OrdinalIgnoreCase);
				if (isTypeSuffixWithController)
				{
					isTypeController = true;
				}
			}
			if (isTypeController)
			{
				if (IsMvcControllerDefaultBeBrServiceController)
				{
					var nonServiceControllerAttribute = type.GetCustomAttribute<NotBrServiceControllerAttribute>();
					if (nonServiceControllerAttribute != null)
					{
						isTypeController = false;
					}
				}
				else
				{
					var serviceControllerAttribute = type.GetCustomAttribute<BrServiceControllerAttribute>();
					if (serviceControllerAttribute == null)
					{
						// !!!
						isTypeController = false;
						// !!!
					}
				}

				if (isTypeController)
				{
					// !!!
					controllerTypes.AddUnique(type);
					// !!!
				}
			}
		}
		return controllerTypes;
	}


	protected async Task<bool> DidProcessApiRequestInfoAsync(
		ApiRequestInfo<JsonElement> apiRequestInfo, ClientConnectionInfo clientConnectionInfo, CancellationToken cancellationToken)
	{
		var apiResponseInfo = await InvokeApiWithApiRequestInfoAsync(
			apiRequestInfo,
			clientConnectionInfo,
			null,
			cancellationToken
		);
		if (apiResponseInfo == null)
		{
			return false;
		}
		return true;
	}

	internal bool DidIsClientVersionValid(string? clientVersion)
	{
		if (ToIsClientVersionSupported?.Invoke(clientVersion) == false)
		{
			return false;
		}
		return true;
	}

#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1822 // 将成员标记为 static
	protected async Task DidConfigFileChangedAsync(ConfigFile configFile)
#pragma warning restore CA1822 // 将成员标记为 static
#pragma warning restore IDE0079 // 请删除不必要的忽略
	{
		// _serviceDiscoverer.ServiceNodeInfo = this.ServiceNodeInfo;
		await Task.CompletedTask;
	}

#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1822 // 将成员标记为 static
	internal void DidApiResponseSent(
#pragma warning restore CA1822 // 将成员标记为 static
#pragma warning restore IDE0079 // 请删除不必要的忽略
		ApiResponseInfoAbstract apiResponseSent,
		Exception? sentException
	)
	{
		if (
			apiResponseSent.ApiResponseSentCompletionSource
			is not TaskCompletionSource<bool> apiResponseSentCompletionSource
		)
		{
			return;
		}

		if (sentException == null)
		{
			// !!!
			apiResponseSentCompletionSource.SetResult(true);
			// !!!
		}
		else
		{
			// !!!
			apiResponseSentCompletionSource.SetResult(false);
			// !!!
		}
	}

	protected void DidSendHeartbeatToApplications(CancellationToken cancellationToken)
	{
		var clientConnectionInfes = BrClientConnectionInfoController.ClientConnectionInfes;
		if (clientConnectionInfes.Count < 1)
		{
			return;
		}

		var serviceConfig = Config;
		foreach (var clientConnectionInfo in clientConnectionInfes)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			clientConnectionInfo.TryAddHeartbeatApiResponseInfoToSendToClient(
				this,
				serviceConfig
			);
		}
	}

	protected void DidPolymerizeRequestPipeTimeInfes()
	{
		if (!Monitor.TryEnter(_requestStatisticsInfesOrderedByRequestTimeAsc))
		{
			return;
		}
		try
		{
			////////////////////////////////////////////////
			// 1/5，清理数据：
			////////////////////////////////////////////////
			var requestStatisticsInLiveSeconds = Config.RequestStatisticsInLiveSeconds;
			var requestStatisticsCreateTimMin = DateTime.Now.AddSeconds(
				-1 * requestStatisticsInLiveSeconds
			);
			if (requestStatisticsInLiveSeconds > 0)
			{
				for (
					var requestStatisticsInfoIndex = 0;
					requestStatisticsInfoIndex
						< _requestStatisticsInfesOrderedByRequestTimeAsc.Count;
					requestStatisticsInfoIndex++
				)
				{
					var requestStatisticsInfo =
						_requestStatisticsInfesOrderedByRequestTimeAsc[
							requestStatisticsInfoIndex
						];
					if (
						requestStatisticsInfo.StatisticsInSecondTime.IsEarlierInSecond(
							requestStatisticsCreateTimMin
						)
					)
					{
						// 清理已过期的统计信息。
						_requestStatisticsInfesOrderedByRequestTimeAsc.RemoveRange(
							0,
							requestStatisticsInfoIndex
						);
						break;
					}
				}
			}

			////////////////////////////////////////////////
			// 2/5，聚合数据：
			////////////////////////////////////////////////
			var requestPipeTimeInfesNotCompleted = new List<RequestPipeTimeInfo>();
			while (_requestPipeTimeInfes.TryDequeue(out var requestPipeTimeInfo))
			{
				// 清理已过期的原始信息：
				if (
					requestPipeTimeInfo.CreateTime.IsEarlierInSecond(
						requestStatisticsCreateTimMin
					)
				)
				{
					continue;
				}
				// 跳过还未完成的统计信息：
				if (!requestPipeTimeInfo.IsCompleted)
				{
					requestPipeTimeInfesNotCompleted.Add(requestPipeTimeInfo);
					continue;
				}

				var requestPipeTimeInfoType = requestPipeTimeInfo.Type;
				var requestPipeStartTime = requestPipeTimeInfo.StartTime;
				var requestPipeCompletedTime = requestPipeTimeInfo.CompletedTime;
				ServiceRequestStatisticsInfo? requestStatisticsInfoForRequestBeginTime =
					null;
				ServiceRequestStatisticsInfo? requestStatisticsInfoForResponseEndTime =
					null;
				////////////////////////////////////////////////
				// 1/2，依据“请求开始时间”的数据聚合：
				////////////////////////////////////////////////
				for (var requestStatisticsInfoIndex = _requestStatisticsInfesOrderedByRequestTimeAsc.Count - 1;
					// 注意这里是“>= -1”。
					requestStatisticsInfoIndex >= -1;
					requestStatisticsInfoIndex--)
				{
					var requestStatisticsInfoExisted = requestStatisticsInfoIndex >= 0
						? _requestStatisticsInfesOrderedByRequestTimeAsc[requestStatisticsInfoIndex]
						: null;
					if (requestStatisticsInfoExisted != null
						&& requestStatisticsInfoExisted.RequestType != requestPipeTimeInfoType)
					{
						continue;
					}

					if (requestStatisticsInfoExisted?.StatisticsInSecondTime.EqualsInSecond(requestPipeStartTime) == true)
					{
						// !!!
						requestStatisticsInfoForRequestBeginTime =
							requestStatisticsInfoExisted;
						// !!!
					}
					else if (requestStatisticsInfoExisted?.StatisticsInSecondTime.IsEarlierInSecond(requestPipeStartTime) == true)
					{
						// !!!
						requestStatisticsInfoForRequestBeginTime = new ServiceRequestStatisticsInfo(requestPipeTimeInfoType,
								// 注意这里使用的是“StartTime”：
								requestPipeStartTime);
						_requestStatisticsInfesOrderedByRequestTimeAsc.Insert(
							requestStatisticsInfoIndex + 1, requestStatisticsInfoForRequestBeginTime);
						// !!!
					}
					else if (requestStatisticsInfoIndex <= 0)
					{
						// !!!
						requestStatisticsInfoForRequestBeginTime =
							new ServiceRequestStatisticsInfo(
								requestPipeTimeInfoType,
								// 注意这里使用的是“StartTime”：
								requestPipeStartTime);
						_requestStatisticsInfesOrderedByRequestTimeAsc.Insert(
							0,
							requestStatisticsInfoForRequestBeginTime
						);
						// !!!
					}

					if (requestStatisticsInfoForRequestBeginTime != null)
					{
						break;
					}
				}

				///////////////////////////////////////////////
				// 2/2，依据“请求完成时间”的数据聚合：
				///////////////////////////////////////////////
				for (var requestStatisticsInfoIndex = _requestStatisticsInfesOrderedByRequestTimeAsc.Count - 1;
					// 注意这里是“>= -1”。
					requestStatisticsInfoIndex >= -1; requestStatisticsInfoIndex--)
				{
					var requestStatisticsInfoExisted = requestStatisticsInfoIndex >= 0
							? _requestStatisticsInfesOrderedByRequestTimeAsc[requestStatisticsInfoIndex]
							: null;
					if (requestStatisticsInfoExisted != null && requestStatisticsInfoExisted.RequestType != requestPipeTimeInfoType)
					{
						continue;
					}

					if (requestStatisticsInfoForResponseEndTime == null)
					{
						if (requestStatisticsInfoExisted?.StatisticsInSecondTime.EqualsInSecond(requestPipeCompletedTime) == true)
						{
							// !!!
							requestStatisticsInfoForResponseEndTime = requestStatisticsInfoExisted;
							// !!!
						}
						else if (requestStatisticsInfoExisted?.StatisticsInSecondTime.IsEarlierInSecond(requestPipeCompletedTime) == true)
						{
							// !!!
							requestStatisticsInfoForResponseEndTime =
								new ServiceRequestStatisticsInfo(
									requestPipeTimeInfoType,
									// 注意这里使用的是“CompletedTime”：
									requestPipeCompletedTime
								);
							_requestStatisticsInfesOrderedByRequestTimeAsc.Insert(
								requestStatisticsInfoIndex + 1,
								requestStatisticsInfoForResponseEndTime
							);
							// !!!
						}
						else if (requestStatisticsInfoIndex <= 0)
						{
							// !!!
							requestStatisticsInfoForResponseEndTime =
								new ServiceRequestStatisticsInfo(
									requestPipeTimeInfoType,
									// 注意这里使用的是“CompletedTime”：
									requestPipeCompletedTime
								);
							_requestStatisticsInfesOrderedByRequestTimeAsc.Insert(
								0,
								requestStatisticsInfoForResponseEndTime
							);
							// !!!
						}
					}
				}

				////////////////////////////////////////////////
				requestStatisticsInfoForRequestBeginTime?.AddRequest(
					requestPipeTimeInfo
				);
				requestStatisticsInfoForResponseEndTime?.AddRequest(
					requestPipeTimeInfo
				);
				////////////////////////////////////////////////
			}

			////////////////////////////////////////////////
			// 3/5，更新只读的统计数据：
			////////////////////////////////////////////////
			var requestStatisticsInfesOrderedByRequestTimeAsc =
				_requestStatisticsInfesOrderedByRequestTimeAsc.ToArray();
			{
				// !!!
				RequestStatisticsInfesOrderedByRequestTimeAsc =
					requestStatisticsInfesOrderedByRequestTimeAsc;
				// !!!
			}

			////////////////////////////////////////////////
			// 4/5，尝试更新服务的当前负载：
			////////////////////////////////////////////////
			{
				double serviceLoadRate;
				if (ToGetServiceLoadRate is Func<double> toGetServiceLoadRate)
				{
					serviceLoadRate = toGetServiceLoadRate();
				}
				else
				{
					var requestsCountInLastSeconds = 0;
					if (
						requestStatisticsInfesOrderedByRequestTimeAsc.Length
						> 0
					)
					{
						// 取上一秒的RPS信息，更加准确。
						var lastSeconds = DateTime.Now.AddSeconds(-1);
						ServiceRequestStatisticsInfo? requestStatisticsInfoInLastSeconds =
							null;
						for (
							var requestStatisticsInfoIndex =
								requestStatisticsInfesOrderedByRequestTimeAsc.Length
								- 1;
							requestStatisticsInfoIndex >= 0;
							requestStatisticsInfoIndex--
						)
						{
							var requestStatisticsInfo =
								requestStatisticsInfesOrderedByRequestTimeAsc[
									requestStatisticsInfoIndex
								];
							if (
								requestStatisticsInfo.RequestType
								!= RequestPipeType.ApiInvokeRequest
							)
							{
								continue;
							}

							var requestStatisticsTimeCompareResult =
								requestStatisticsInfo.StatisticsInSecondTime.CompareTo(
									lastSeconds,
									DateTimeField.Second
								);
							if (requestStatisticsTimeCompareResult < 0)
							{
								break;
							}
							else if (
								requestStatisticsTimeCompareResult
								> 0
							)
							{
								continue;
							}
							else
							{
								requestStatisticsInfoInLastSeconds =
									requestStatisticsInfo;
								break;
							}
						}

						if (requestStatisticsInfoInLastSeconds != null)
						{
							// !!!
							requestsCountInLastSeconds =
								requestStatisticsInfoInLastSeconds.RequestsCount;
							// !!!
						}
					}

					var rpsForFullLoadRate = Config.RPSForFullLoadRate;
					if (rpsForFullLoadRate > 0)
					{
						serviceLoadRate =
							requestsCountInLastSeconds
							/ rpsForFullLoadRate;
					}
					else
					{
						serviceLoadRate = 0;
					}
				}
				// !!!
				LoadRate = serviceLoadRate;
				// !!!
			}

			////////////////////////////////////////////////
			// 5/5，尝试持久化统计数据：
			////////////////////////////////////////////////
			{
				ToSaveServiceRequestStatisticsInfes?.Invoke(
					RequestStatisticsInfesOrderedByRequestTimeAsc
				);
			}
		}
		catch (Exception exception)
		{
			ExceptionLogFile?.Logs(this, "聚合请求统计信息失败，程序异常。", exception);
		}
		finally
		{
			Monitor.Exit(_requestStatisticsInfesOrderedByRequestTimeAsc);
		}
	}

	#endregion


	////////////////////////////////////////////////
	// @实现“Middleware”定义方法
	////////////////////////////////////////////////

	#region 实现“Middleware”

	protected override async Task<bool> DidIfGoToNextMiddlewareByInvokeAsync(HttpContext httpContext)
	{
		if (await this.TryToProcessRequestInContextAsync(httpContext))
		{
			return false;
		}
		return true;
	}

	#endregion
}
