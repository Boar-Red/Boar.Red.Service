using BaoXia.Utils.ConcurrentTools;
using BaoXia.Utils.Interfaces;
using BaoXia.Utils.Models;
using Boar.Red.Service.BrService.Constants;
using Boar.Red.Service.BrService.Interfaces;
using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.BrService.ViewModels;
using System.Collections.Concurrent;
using System.Reflection;

namespace Boar.Red.Service.BrService;

public class Client
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	protected readonly string? _groupName;

	public string? GroupName => _groupName;

	protected readonly string _serviceName;

	public string ServiceName => _serviceName;

	public string? ServiceVersionMin { get; set; }
	public string? ServiceVersionMax { get; set; }

	protected string? _version = null;
	public string? Version
	{
		get
		{
			if (_version == null
				&& Assembly.GetEntryAssembly()?.GetName()?.Version is Version entryAssemblyVersion)
			{
				_version = entryAssemblyVersion.ToString();
			}
			return _version;
		}
	}

	protected object _serviceUriLocker = new();

	protected string? _serviceUri;

	/// <summary>
	/// ⚠一个客户端连接只能连接一个服务端，否则无法安全关机。 ⚠
	/// </summary>
	public string? ServiceUri
	{
		get
		{
			if (!string.IsNullOrEmpty(_serviceUri))
			{
				return _serviceUri;
			}
			return Config?.ServiceUri;
		}
		set
		{
			_serviceUri = value;
		}
	}

	public ServiceDiscoverer? ServiceDiscoverer { get; init; }


	protected string? _applicationIdentity;

	public string? ApplicationIdentity
	{
		get
		{
			if (!string.IsNullOrEmpty(_applicationIdentity))
			{
				return _applicationIdentity;
			}
			return Config?.ApplicationIdentity;
		}
		set
		{
			_applicationIdentity = value;
		}
	}


	protected IServiceApplicationInfo? _serviceApplicationInfo = null;

	protected SemaphoreSlim _serviceApplicationLocker = new(1);


	protected string? _applicationPassword;

	public string? ApplicationPassword
	{
		get
		{
			if (!string.IsNullOrEmpty(_applicationPassword))
			{
				return _applicationPassword;
			}
			return Config?.ApplicationPassword;
		}
		set
		{
			_applicationPassword = value;
		}
	}


	protected string? _applicationAesKey;

	public string ApplicationAesKey
	{
		get
		{
			var aesKey = _applicationAesKey;
			if (string.IsNullOrEmpty(aesKey))
			{
				aesKey = Config?.ApplicationAesKey;
			}
			if (string.IsNullOrEmpty(aesKey))
			{
				throw new Exception($"当前客户端没有指定有效的加密密钥（“{nameof(ApplicationAesKey)}”）。");
			}
			return aesKey;
		}
		set
		{
			_applicationAesKey = value;
		}
	}


	protected double _clientConnectionOpenTimeoutSeconds;

	public double ClientConnectionOpenTimeoutSeconds
	{
		get
		{
			var clientConnectionOpenTimeoutSeconds
			    = _clientConnectionOpenTimeoutSeconds;
			if (clientConnectionOpenTimeoutSeconds <= 0)
			{
				if (Config is { } config)
				{
					clientConnectionOpenTimeoutSeconds
					    = config.ClientConnectionOpenTimeoutSeconds;
				}
			}
			if (clientConnectionOpenTimeoutSeconds <= 0)
			{
				clientConnectionOpenTimeoutSeconds
				    = TimeoutSecondsDefault.ClientConnectionOpen;
			}
			return clientConnectionOpenTimeoutSeconds;
		}
		set
		{
			_clientConnectionOpenTimeoutSeconds = value;
		}
	}

	public Func<ClientConfig?>? ToGetClientConfig { get; set; }

	public ClientConfig? Config
	{
		get
		{
			var toGetClientConfig = ToGetClientConfig;
			if (toGetClientConfig != null)
			{
				return toGetClientConfig();
			}
			return ClientConfig.Default;
		}
	}

	public ILogFile? LogFile { get; set; }
	public ILogFile? ExceptionLogFile { get; set; }
	public ILogFile? WarningLogFile { get; set; }
	public ILogFile? BrServiceLogFile { get; set; }


	/// <summary>
	/// 已注册的，需要监听源自服务端发起的响应数据，关键字为“自定义的请求Id”，
	/// 【注意】“自定义的请求Id”不能为纯数字的。
	/// </summary>
	protected ConcurrentDictionary<string, ApiRequestInfo<object>> _apiRequestInfesNeedResponseOriginateService = new();


	protected ClientConnection _currentConnection;

	public ClientState State
	{
		get
		{
			return _currentConnection.State;
		}
	}

	public bool IsClosing
	{
		get
		{
			return _currentConnection.IsClosing;
		}
	}

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public Client(string? groupName, string serviceName, string? serviceUri, ServiceDiscoverer? serviceDiscoverer,
	    string? applicationIdentity, string? applicationPassword, string? applicationAesKey, Func<ClientConfig?>? toGetClientConfig,
	    ILogFile? logFile, ILogFile? exceptionLogFile, ILogFile? warningLogFile, ILogFile? brServiceLogFile)
	{
		_groupName = groupName;
		_serviceName = serviceName;

		ServiceUri = serviceUri;
		ServiceDiscoverer = serviceDiscoverer;
		ServiceDiscoverer?.AddServiceNeedFound(
		    _groupName,
		    _serviceName,
		    DidServiceInfoFoundByServiceDiscoverer);

		ApplicationIdentity = applicationIdentity;
		ApplicationPassword = applicationPassword;
		_applicationAesKey = applicationAesKey;

		ToGetClientConfig = toGetClientConfig;

		LogFile = logFile;
		ExceptionLogFile = exceptionLogFile;
		WarningLogFile = warningLogFile;
		BrServiceLogFile = brServiceLogFile;

		// !!!
		_currentConnection = new ClientConnection(this);
		// !!!
	}

	public async Task<IServiceApplicationInfo> GetServiceApplicationInfoAsync(int clientUserId, ClientIpInfo clientIpInfo)
	{
		if (_serviceApplicationInfo != null)
		{
			return _serviceApplicationInfo;
		}

		var serviceApplication = await AsyncLock.LockAsync(null, () => _serviceApplicationLocker, async (_) =>
		{
			if (_serviceApplicationInfo != null)
			{
				return _serviceApplicationInfo;
			}

			var applicationIdentity = ApplicationIdentity;
			if (string.IsNullOrEmpty(applicationIdentity))
			{
				throw new InvalidOperationException($"指定当前服务应用程序（{nameof(ApplicationIdentity)}）的身份标识符为“空”。");
			}

			var serviceApplication = await DidGetServiceApplicationWithServiceApplicationIdentityAsync(
			applicationIdentity, clientUserId, clientIpInfo);
			{ }
			return serviceApplication;
		});
		{
			_serviceApplicationInfo = serviceApplication;
		}
		return _serviceApplicationInfo!;
	}

	public async Task<int> GetServiceApplicationIdAsync(
	    int clientUserId,
	    ClientIpInfo clientIpInfo)
	{
		var serviceApplicationInfo
		    = await GetServiceApplicationInfoAsync(
		    clientUserId,
		    clientIpInfo);
		{ }
		return serviceApplicationInfo.Id;
	}

	public async Task<ApiResponseInfo<MethodResultType>> InvokeApiToGetApiResponseInfoAsync<MethodResultType>(
	    string? methodName,
	    object? methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams,
	    //
	    DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.AesCtr,
	    //
	    string? apiRequestIdentity = null)
	{
		return await _currentConnection.InvokeApiToGetApiResponseInfoAsync<MethodResultType>(
		    methodName,
		    methodParams,
		    methodExtensionParams,
		    //
		    encryptionMethod,
		    //
		    apiRequestIdentity);
	}

	public async Task<ApiResponseInfo<object>> InvokeApiToGetBytesAsync(
	    string? methodName,
	    object? methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams,
	    //
	    DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.AesCtr)
	{
		return await _currentConnection.InvokeApiToGetBytesAsync(
		    methodName,
		    methodParams,
		    methodExtensionParams,
		    //
		    encryptionMethod);
	}

	public async Task<ApiResponseInfo<string>> InvokeApiToGetStringAsync(
	    string? methodName,
	    object? methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams,
	    //
	    DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.AesCtr)
	{
		return await _currentConnection.InvokeApiToGetStringAsync(
		    methodName,
		    methodParams,
		    methodExtensionParams,
		    //
		    encryptionMethod);
	}

	public async Task<ApiResponseInfo<ObjectType>> InvokeApiToGetObjectAsync<ObjectType>(
	    string? methodName,
	    object? methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams,
	    //
	    DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.AesCtr)
	{
		return await _currentConnection.InvokeApiToGetObjectAsync<ObjectType>(
		    methodName,
		    methodParams,
		    methodExtensionParams,
		    //
		    encryptionMethod);
	}

	public async Task<ApiResponseInfo<ObjectType>> InvokeApiToGetObjectAsync<ObjectType>(
	    string? methodName,
	    object? methodParams,
	    DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.None,
	    params DataPacketSection[] methodExtensionParams)
	{
		return await _currentConnection.InvokeApiToGetObjectAsync<ObjectType>(
		    methodName,
		    methodParams,
		    encryptionMethod,
		    methodExtensionParams);
	}

	public void ListenToProcessApiResponseOriginateService<MethodResultType>(
	    string? requestIdentity,
	    string? methodName,
	    //
	    Func<ApiResponseInfo<MethodResultType>?, Task> toListenApiResponseOriginateServiceAsync)
	    where MethodResultType : class
	{
		if (string.IsNullOrEmpty(requestIdentity))
		{
			return;
		}
		if (string.IsNullOrEmpty(methodName))
		{
			return;
		}

		var apiRequestInfo
		    = _apiRequestInfesNeedResponseOriginateService.GetOrAdd(
		requestIdentity,
		    new ApiRequestInfo<object>(
			ApplicationIdentity ?? string.Empty,
			requestIdentity,
			methodName,
			null,
			null));
		{
			////////////////////////////////////////////////
			// !!!
			apiRequestInfo.ApiResponseInfoType = typeof(ApiResponseInfo<MethodResultType>);
			apiRequestInfo.ToReceiveServiceOriginalApiResponseAsync = async (apiResponse) =>
			{
				await toListenApiResponseOriginateServiceAsync(apiResponse as ApiResponseInfo<MethodResultType>);
			};
			// !!!
			////////////////////////////////////////////////
		}
	}

	internal bool TryGetApiRequestInfoNeedResponseOriginateService(
	    string? requestIdentity,
	    out ApiRequestInfo<object>? apiRequestInfo)
	{
		// !!!
		apiRequestInfo = null;
		// !!!

		if (string.IsNullOrEmpty(requestIdentity))
		{
			return false;
		}

		if (_apiRequestInfesNeedResponseOriginateService.TryGetValue(
		    requestIdentity,
		    out apiRequestInfo))
		{
			return true;
		}

		return false;
	}

	public async Task CloseAsync(double closeTimeoutSeconds = 0.0)
	{
		await _currentConnection.CloseAsync(closeTimeoutSeconds);
	}

	public async Task<bool> ShutDownServiceAsync()
	{
		return await _currentConnection.ShutDownServiceAsync();
	}

	#endregion


	////////////////////////////////////////////////
	// @事件节点
	////////////////////////////////////////////////

	#region 事件节点

	/// <summary>
	/// 事件节点，服务信息被服务发现器发现，客户端相关的服务发生了变化，
	/// 此时需要安全的断开现有的连接，重新按照服务配置信息，重新连接服务。
	/// </summary>
	/// <param name="serviceNeedFoundInfo">之前注册的需要发现的服务信息。</param>
	protected void DidServiceInfoFoundByServiceDiscoverer(ServiceNeedFoundInfo serviceNeedFoundInfo)
	{
		var lastClientConnection = _currentConnection;
		{
			_currentConnection = new ClientConnection(this);
		}
		_ = Task.Run(async () =>
		{
			await lastClientConnection.CloseAsync();
		});
	}

	protected async Task<IServiceApplicationInfo> DidGetServiceApplicationWithServiceApplicationIdentityAsync(
	    string serviceApplicationIdentity, int clientUserId, ClientIpInfo clientIpInfo)
	{
		var serviceApplicationManager = BaoXia.Utils.Environment.GetService<IServiceApplicationManager>()
			?? throw new NotImplementedException($"无法获取“{nameof(IServiceApplicationManager)}”服务实例，无法获取服务应用程序信息。");
		var serviceApplication = await serviceApplicationManager.GetServiceApplicationWithIdentityAsync(
		    serviceApplicationIdentity, clientUserId, clientIpInfo)
			?? throw new InvalidOperationException($"无法获取到服务应用程序（{serviceApplicationIdentity}）信息。");
		{ }
		return serviceApplication;
	}

	#endregion
}