using BaoXia.Constants;
using BaoXia.Utils;
using BaoXia.Utils.ConcurrentTools;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Models;
using Boar.Red.Service.BrService.Constants;
using Boar.Red.Service.BrService.Controllers;
using Boar.Red.Service.BrService.Extensions;
using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.BrService.Models.StatisticsInfo;
using Boar.Red.Service.BrService.ViewModels;
using Boar.Red.Service.Utils;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using static Boar.Red.Service.BrService.Constants.NamingConventions;

namespace Boar.Red.Service.BrService;

public class ClientConnection : IDisposable
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	private int _id;
	public int Id
	{
		get
		{
			return _id;
		}
	}

	protected Client _client;

	protected ClientState _state = ClientState.Unknown;

	public ClientState State
	{
		get
		{
			return _state;
		}
		set
		{
			lock (this)
			{
				if (_state == value)
				{
					return;
				}
				var lastState = _state;
				_state = value;
				// !!!
				DidStateChangedFrom(lastState, value);
				// !!!
			}
		}
	}

	public bool IsClosing
	{
		get
		{
			switch (_state)
			{
				case ClientState.ServiceConnecting:
				case ClientState.ServiceConnected:
					{
						return false;
					}
				case ClientState.Unknown:
				case ClientState.Closing:
				case ClientState.Closed:
					{
						return true;
					}
			}
			return true;
		}
	}

	protected bool _isCloseConnectionApiInvoked = false;
	protected bool _isCloseConnectionApiResponsed = false;

	protected SemaphoreSlim _serviceUriLocker = new(1);
	protected string? _serviceUri;

	protected int _apiResponseReceiveBufferSizeInByte;
	public int ApiResponseReceiveBufferSizeInByte
	{
		get
		{
			var apiResponseReceiveBufferSizeInByte
			    = _apiResponseReceiveBufferSizeInByte;
			if (apiResponseReceiveBufferSizeInByte <= 0)
			{
				if (_client.Config is { } config)
				{
					apiResponseReceiveBufferSizeInByte
					    = config.ApiResponseReceiveBufferSizeInByte;
				}
			}
			if (apiResponseReceiveBufferSizeInByte <= 0)
			{
				apiResponseReceiveBufferSizeInByte
				    = ReceiveBufferSizeInByteDefault.ReceiveServiceResponse;
			}
			return apiResponseReceiveBufferSizeInByte;
		}
		set
		{
			_apiResponseReceiveBufferSizeInByte = value;
		}
	}

	public ClientConfig? ConfigFromService { get; set; }

	public int ClientRequestDataPacketSizeToCompression
	{
		get
		{
			if (_client.Config is ClientConfig config
			    && config.ClientRequestDataPacketSizeToCompression != 0)
			{
				return config.ClientRequestDataPacketSizeToCompression;
			}
			if (ConfigFromService != null)
			{
				return ConfigFromService.ClientRequestDataPacketSizeToCompression;
			}
			return 0;
		}
	}

	public string? ClientRequestDataPacketCompressionMethodName
	{
		get
		{
			if (_client.Config is ClientConfig config
			    && !string.IsNullOrEmpty(config.ClientRequestDataPacketCompressionMethodName))
			{
				return config.ClientRequestDataPacketCompressionMethodName;
			}
			if (ConfigFromService != null)
			{
				return ConfigFromService.ClientRequestDataPacketCompressionMethodName;
			}
			return null;
		}
	}

	public System.Text.Encoding ClientRequestTextEncoding
	{
		get
		{
			if (_client.Config is ClientConfig config
			    && !string.IsNullOrEmpty(config.ClientRequestTextEncodingName))
			{
				return config.ClientRequestTextEncoding;
			}
			if (ConfigFromService != null)
			{
				return ConfigFromService.ClientRequestTextEncoding;
			}
			return DataPacketTextEncoding.Default;
		}
	}

	public System.Text.Encoding ServiceResponseTextEncoding
	{
		get
		{
			if (_client.Config is ClientConfig config
			    && !string.IsNullOrEmpty(config.ServiceResponseTextEncodingName))
			{
				return config.ServiceResponseTextEncoding;
			}
			if (ConfigFromService != null)
			{
				return ConfigFromService.ServiceResponseTextEncoding;
			}
			return DataPacketTextEncoding.Default;
		}
	}


	////////////////////////////////////////////////

	protected CancellationTokenSource _cancellationTokenSource = new();

	public List<SocketInfo> _allSocketInfesToService = [];

	public ConcurrentQueue<SocketInfo> _idleSocketInfesSendToService = new();

	protected bool IsAnySocketNeedSendHeartbeta
	{
		get
		{
			var isAnySocketNeedSendHeartbeta = false;
			foreach (var idleSocketInfo in _idleSocketInfesSendToService)
			{
				if (idleSocketInfo.HeartbeatApiResponseInfoNeedSend != null)
				{
					// !!!
					isAnySocketNeedSendHeartbeta = true;
					break;
					// !!!
				}
			}
			return isAnySocketNeedSendHeartbeta;
		}
	}

	public ConcurrentQueue<SocketInfo> _idleSocketInfesReceiveFromService = new();
	public int _socketInfesToServiceInCreatingCount;
	public int _socketInfesToServiceInCreatedCount;
	public int _socketInfesToServiceInClosedCount;

	public bool IsAnySocketConnectionEnable
	{
		get
		{
			if (_socketInfesToServiceInCreatingCount > 0)
			{
				return true;
			}
			lock (_allSocketInfesToService)
			{
				foreach (var socketInfoToService in _allSocketInfesToService)
				{
					if (socketInfoToService.IsConnectionEnable)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public bool IsNoneSocketConnectionEnable
	{
		get
		{
			return !IsAnySocketConnectionEnable;
		}
	}

	protected ConcurrentQueue<ApiRequestInfo<object>> _apiRequestInfesNeedSend = new();

	protected Tasks _tasksToSendApiRequest;

	protected ConcurrentDictionary<string, ApiRequestInfo<object>> _apiRequestInfesNeedResponse = new();

	protected Tasks _tasksToReceiveApiResponse;

	protected ConcurrentQueue<ApiResponseInfoBytesInfo> _apiResponseInfesNeedProcess = new();

	public bool IsClientIdle
	{
		get
		{
			// 是否存在：等待发送的请求信息。
			if (_apiRequestInfesNeedSend.IsEmpty != true)
			{
				return false;
			}
			// 是否存在：等待响应的请求信息。
			if (_apiRequestInfesNeedResponse.IsEmpty != true)
			{
				return false;
			}
			// 是否存在：等待处理的响应信息。
			if (_apiResponseInfesNeedProcess.IsEmpty != true)
			{
				return false;
			}
			// 是否存在：使用中的Socket
			lock (_allSocketInfesToService)
			{
				if (_allSocketInfesToService.Count != _idleSocketInfesSendToService.Count)
				{
					return false;
				}
				// Socket 一直会监听接收来自服务端的数据，
				// 因此不可用作空闲判断，
				// 监听的空闲状态，只可以根据功能逻辑来判断。
				// _allSocketInfesToService.Count = _idleSocketInfesReceiveFromService.Count
			}
			return true;
		}
	}
	public bool IsClientDisconnected
	{
		get
		{
			// 是否当前客户端是空闲的。
			var isClientIdle = IsClientIdle;
			if (!isClientIdle)
			{
				return false;
			}

			// 是否存在连接中的Socket：
			var isNoneSocketConnectionEnable
			    = IsNoneSocketConnectionEnable;
			if (!isNoneSocketConnectionEnable)
			{
				return false;
			}
			return true;
		}
	}

	protected Tasks _tasksToProcessApiResponse;

	protected LoopTask _taskToQueueHeartbeatRequestInfoToService;

	public ApplicationInfo? ApplicationInfo { get; set; }

	public int ConnectionsCountMax
	{
		get
		{
			var connectionsCountMax = 1;
			var applicationInfo = ApplicationInfo;
			if (applicationInfo != null)
			{
				connectionsCountMax = applicationInfo.ConnectionsCountMax;
			}
			return connectionsCountMax;
		}
	}

	private readonly ConcurrentQueue<ClientRequestPipeTimeInfo> _requestPipeTimeInfes = new();


	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////


	#region 自身实现

	public ClientConnection(Client client)
	{
		_client = client;

		////////////////////////////////////////////////
		State = ClientState.ServiceConnecting;
		////////////////////////////////////////////////

		_tasksToSendApiRequest = new(() =>
		{
			return ConnectionsCountMax;
		});
		_tasksToReceiveApiResponse = new(() =>
		{
			return ConnectionsCountMax;
		});
		_tasksToProcessApiResponse = new(() =>
		{
			var responseProcessThreadsCountMax = _client.Config?.ResponseProcessThreadsCountMax ?? 1;
			if (responseProcessThreadsCountMax < 1)
			{
				responseProcessThreadsCountMax = ClientConfig.ResponseProcessThreadsCountMaxDefault;
			}
			return responseProcessThreadsCountMax;
		});

		_taskToQueueHeartbeatRequestInfoToService = new((cancellationToken) =>
		{
			// !!!⚠ 关机后，不再发送心跳包。 ⚠!!!
			if (IsClosing)
			{
				return false;
			}

			// !!!
			DidQueueHeartbeatRequestInfoToService(cancellationToken);
			// !!!
			return true;
		},
		() =>
		{
			var intervalSecond
	    = _client.Config?.SocketConnectionHeartbeatIntervalSecondsFinally
	    ?? TimerIntervalSeconds.SocketConnectionHeartbeatIntervalSecondsMin;
			if (intervalSecond <= 0)
			{
				intervalSecond
		    = TimerIntervalSeconds.SocketConnectionHeartbeatIntervalSecondsMin;
			}
			return intervalSecond;
		});
	}

	~ClientConnection()
	{
		_cancellationTokenSource.Cancel();
		while (_idleSocketInfesSendToService.TryDequeue(out var socketInfoToService))
		{
			socketInfoToService?.Dispose();
		}
		lock (_allSocketInfesToService)
		{
			_allSocketInfesToService.Clear();
		}
	}

	protected void AddClientRequestPipeTimeInfo(ClientRequestPipeTimeInfo? requestPipeTimeInfo)
	{
		if (requestPipeTimeInfo == null)
		{
			return;
		}

		var clientConfig = _client.Config;
		if (clientConfig == null
		    || clientConfig.RequestStatisticsInLiveSeconds <= 0)
		{
			return;
		}

		_requestPipeTimeInfes.Enqueue(requestPipeTimeInfo);
	}

	protected async Task WaitAllRequestCompletedForCloseAsync(double timeoutSeconds)
	{
		var timeoutCancellationTokenSource = new CancellationTokenSource();
		var timeoutCancellationTokenToken = timeoutCancellationTokenSource.Token;
		try
		{
			////////////////////////////////////////////////
			DebugInfo.WriteLine("关机测试", "客户端",
			    $"WaitAllRequestCompletedForCloseAsync，关机开始，{_socketInfesToServiceInClosedCount}/{_socketInfesToServiceInCreatedCount} 个Socket连接需要关闭。");
			////////////////////////////////////////////////

			if (timeoutSeconds > 0.0)
			{
				timeoutCancellationTokenSource.CancelAfter((int)(timeoutSeconds * 1000.0));
			}

			while (!timeoutCancellationTokenToken.IsCancellationRequested)
			{
				if (IsClientDisconnected)
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
			if (exception is OperationCanceledException operationCanceledException
			    && operationCanceledException.CancellationToken == timeoutCancellationTokenToken)
			{
				_client.ExceptionLogFile?.Logs(
				    this,
				    "客户端安全关闭，操作失败，关闭操作超过了 " + timeoutSeconds + " 秒。",
				    exception);
			}
			else
			{
				_client.ExceptionLogFile?.Logs(
				    this,
				    "客户端安全关闭，操作失败，程序异常。",
				    exception);
			}
		}
		finally
		{
			lock (this)
			{
				////////////////////////////////////////////////
				State = ClientState.Closed;
				//
				_client.LogFile?.Logs(this, "客户端安全关闭，操作成功。", null);

				////////////////////////////////////////////////
				DebugInfo.WriteLine(
				    "关机测试",
				    "客户端",
				    $"WaitAllRequestCompletedForCloseAsync，关机完成，Socket连接已关闭 {_socketInfesToServiceInClosedCount}/{_socketInfesToServiceInCreatedCount} 个（关闭/创建）。");
				////////////////////////////////////////////////
			}
		}
	}

	public async Task CloseAsync(double closeTimeoutSeconds = 0.0)
	{
		if (IsClosing)
		{
			return;
		}
		lock (this)
		{
			if (IsClosing)
			{
				return;
			}
			// !!!
			State = ClientState.Closing;
			DebugInfo.WriteLine("关机测试", "客户端", "CloseAsync，开始。");
			// !!!
		}

		// !!! 尝试处理Api响应，避免空载时，无法出发关机事件 !!!
		TryToProcessApiResponse();
		// !!!

		// !!!
		await WaitAllRequestCompletedForCloseAsync(closeTimeoutSeconds);
		// !!!
		DebugInfo.WriteLine($"关机测试", "客户端。", "CloseAsync，结束。");
		// !!!
	}

	public async Task<bool> ShutDownServiceAsync()
	{
		if (IsClosing)
		{
			return true;
		}
		lock (this)
		{
			if (IsClosing)
			{
				return true;
			}
			// !!!
			State = ClientState.Closing;
			DebugInfo.WriteLine(
			    "关机测试",
			    "客户端",
			    "ShutDownServiceAsync，开始。");
			// !!!
		}

		var apiResponse = await InvokeApiToGetApiResponseInfoAsync<object>(
		    ServiceMethodNames.ServiceShutdown,
		    null,
		    null,
		    DataPackageEncryptionMethod.None,
		    ApiRequestIdentity.Service.Shutdown);
		if (!ApiRequestIdentity.Service.Shutdown.EqualsIgnoreCase(
		    apiResponse?.RequestIdentity))
		{
			return false;
		}
		return true;
	}

	protected async Task<SocketInfo> GetSocketToSendApiRequestInfoAsync(
	    bool isInUdpClientLockSection,
	    bool isCreateNewSocketDisable)
	{
		string? openConnectionError = null;
		string? openConnectionErrorDescription = null;
		Exception? openConnectionException = null;
		try
		{
			////////////////////////////////////////////////
			// 1/，尝试获取空闲，有效的与服务端的连接：
			////////////////////////////////////////////////
			SocketInfo? socketInfoToService = null;
			while (_idleSocketInfesSendToService.TryDequeue(out socketInfoToService))
			{
				if (socketInfoToService.IsConnectionEnable == true)
				{
					break;
				}
				else
				{
					lock (_allSocketInfesToService)
					{
						_allSocketInfesToService.Remove(socketInfoToService);
					}
				}
			}
			if (socketInfoToService != null)
			{
				return socketInfoToService;
			}
			if (isCreateNewSocketDisable == true)
			{
				var errorDescription = "获取用于发送Api请求的Socket失败，当前没有可用的连接，并且本次获取被设置为不可新建连接。";
				// !!!
				_client.BrServiceLogFile?.Logs(this, errorDescription, null);
				// !!!

				return new SocketInfo(new ClientWebSocket(), ClientIpInfo.New)
				{
					ErrorToOpenConnection = Error.OperationFailed,
					ErrorDescriptionToOpenConnection = errorDescription
				};
			}


			////////////////////////////////////////////////
			// 2/，获取不到可用连接时，建立新的与服务端的连接：
			////////////////////////////////////////////////
			// !!! 服务关机后，不再创建新的  !!!
			if (IsClosing)
			{
				var errorDescription = "获取用于发送Api请求的Socket失败，服务已关机。";
				// !!!
				_client.BrServiceLogFile?.Logs(this, errorDescription, null);
				// !!!
				return new SocketInfo(new ClientWebSocket(), ClientIpInfo.New)
				{
					ErrorToOpenConnection = Error.OperationFailed,
					ErrorDescriptionToOpenConnection = errorDescription
				};
			}

			var connectionsCountMax = ConnectionsCountMax;
			lock (_allSocketInfesToService)
			{
				////////////////////////////////////////////////
				// !!!⚠ 如果当前没有可用的Socket，则重置连接Id，                ⚠!!!
				// !!!⚠ 避免所有Socket关闭后，服务端注销客户端连接信息。  ⚠!!!
				////////////////////////////////////////////////
				if (_allSocketInfesToService.Count < 1)
				{
					_id = 0;
				}

				if (connectionsCountMax > 0
				    && (_allSocketInfesToService.Count + _socketInfesToServiceInCreatingCount) >= connectionsCountMax)
				{
					var errorDescription = $"获取用于发送Api请求的Socket失败，连接数到达上限（{connectionsCountMax}个）。";
					// !!!
					_client.BrServiceLogFile?.Logs(this, errorDescription, null);
					// !!!
					return new SocketInfo(new ClientWebSocket(), ClientIpInfo.New)
					{
						ErrorToOpenConnection = Error.InvalidRequestByTooMany,
						ErrorDescriptionToOpenConnection = errorDescription
					};
				}
				//
				Interlocked.Increment(ref _socketInfesToServiceInCreatingCount);
				//
			}

			ServiceNodeInfo? serviceNodeInfoInServiceDiscoverer = null;
			ClientWebSocket? socketToService = null;
			int clientConnectionId = _id;
			ApplicationInfo? applicationInfoConnected = null;
			Exception? clientConnectException = null;
			ClientRequestPipeTimeInfo clientConnectRequestPipeTimeInfo
			    = new(RequestPipeType.ClientConnectionOpen);
			// ⏱⏱⏱，【信息记录】
			AddClientRequestPipeTimeInfo(clientConnectRequestPipeTimeInfo);
			// ⏱⏱⏱，【信息记录】
			ClientConfig? configFromService = null;
			//
			WebSocketCloseStatus? webSocketCloseStatusByClient = null;
			string? webSocketCloseStatusDescriptionByClient = null;
			WebSocketCloseStatus? webSocketCloseStatusByService = null;
			string? webSocketCloseStatusDescriptionByService = null;
			//
			var cancelTokenSourceToGetServiceUriByServiceDiscoverer = new CancellationTokenSource();
			var cancelTokenToGetServiceUriByServiceDiscoverer = cancelTokenSourceToGetServiceUriByServiceDiscoverer.Token;
			try
			{
				cancelTokenSourceToGetServiceUriByServiceDiscoverer.CancelAfter(
				    (int)(_client.ClientConnectionOpenTimeoutSeconds * 1000));

				var serviceUriString = _serviceUri ?? _client.ServiceUri;
				if (string.IsNullOrEmpty(serviceUriString)
				    && _client.ServiceDiscoverer is ServiceDiscoverer serviceDiscoverer)
				{
					try
					{
						////////////////////////////////////////////////
						// !!!
						// !!! ⚠ 同一个线程只能获得一次信号量，                     ⚠
						// !!! ⚠ 因此，不能在同一个线程中“Wait”多次，           ⚠
						// !!! ⚠ 后续的“Wait”会因为信号量已经被占用而阻塞。 ⚠
						// !!!
						if (!isInUdpClientLockSection)
						{
							await _serviceUriLocker.WaitAsync();
						}
						// !!!
						////////////////////////////////////////////////

						serviceUriString = _serviceUri ?? _client.ServiceUri;
						if (string.IsNullOrEmpty(serviceUriString))
						{
							serviceNodeInfoInServiceDiscoverer
							    = await serviceDiscoverer.GetServiceNodeInfoToConnectAsync(
							    _client.GroupName,
							    _client.ServiceName,
							    _client.ServiceVersionMin,
							    _client.ServiceVersionMax,
							    //
							    cancelTokenToGetServiceUriByServiceDiscoverer);
							if (serviceNodeInfoInServiceDiscoverer != null)
							{
								// !!!
								serviceUriString = serviceNodeInfoInServiceDiscoverer.FinalBrServiceUri;
								// !!!
							}
						}
					}
					finally
					{
						////////////////////////////////////////////////
						// !!!
						if (!isInUdpClientLockSection)
						{
							_serviceUriLocker.Release();
						}
						// !!!
						////////////////////////////////////////////////

					}
				}

				if (string.IsNullOrEmpty(serviceUriString))
				{
					// !!! 
					webSocketCloseStatusByClient
					    = WebSocketCloseStatus.PolicyViolation;
					webSocketCloseStatusDescriptionByClient
					    = SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_NeedServiceUri;
					////////////////////////////////////////////////
					var errorDescription = $"创建与服务器的连接失败，无法获取服务Uri：{nameof(_serviceUri)} = {_serviceUri}。";
					// !!!
					openConnectionError = Error.OperationFailed;
					openConnectionErrorDescription = errorDescription;
					// !!!
					throw new Exception(errorDescription);
					////////////////////////////////////////////////
				}

				var serviceUri = new Uri(serviceUriString);
				var applicationIdentity = _client.ApplicationIdentity;
				if (string.IsNullOrEmpty(applicationIdentity))
				{
					// !!! 
					webSocketCloseStatusByClient
					    = WebSocketCloseStatus.PolicyViolation;
					webSocketCloseStatusDescriptionByClient
					    = SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_NeedApplicationIdentity;
					////////////////////////////////////////////////
					var errorDescription = $"创建与服务器的连接失败，无法获取应用程序标识符：{nameof(_client.ApplicationIdentity)} = {_client.ApplicationIdentity}。";
					// !!!
					openConnectionError = Error.OperationFailed;
					openConnectionErrorDescription = errorDescription;
					// !!!
					throw new Exception(errorDescription);
					////////////////////////////////////////////////
				}

				// !!! 创建请求对象，应当在连接服务器之前，避免污染连接计时 !!!
				var clientConnectionOpenRequest
				    = new ApiRequestInfo<object>(
				    applicationIdentity,
				    ApiRequestIdentity.New(),
				    ServiceMethodNames.OpenConnection,
				    new BrClientConnectionInfoController.OpenConnectRequest(
					clientConnectionId,
					_client.Version,
					_client.ApplicationIdentity,
					_client.ApplicationPassword),
				    null)
				    {
					    ClientRequestPipeTimeInfo = clientConnectRequestPipeTimeInfo
				    };


				// Console.WriteLine("#连接性能测试#，发送连接请求时间：" + DateTime.Now.ToString("HH:mm:ss fff"));

				// ⏱⏱⏱，【计时埋点】
				clientConnectRequestPipeTimeInfo.SetTime_0_ToConnectService(DateTime.Now);
				// ⏱⏱⏱，【计时埋点】
				socketToService = new ClientWebSocket();
				await socketToService.ConnectAsync(
				    serviceUri,
				    _cancellationTokenSource.Token);

				var applicationAesKey = _client.Config?.ApplicationAesKey ?? string.Empty;

				if (!await socketToService.SendApiRequestInfoAsync(
				    clientConnectionOpenRequest,
				    //
				    _client.Config?.ClientRequestTextEncoding ?? DataPacketTextEncoding.Default,
				    // 服务端此时不知道连接属于哪个应用程序，
				    // 因此无法使用加密算法。
				    DataPackageEncryptionMethod.None,
				    applicationAesKey,
				    ClientRequestDataPacketSizeToCompression,
				    ClientRequestDataPacketCompressionMethodName,
				    //
				    _cancellationTokenSource.Token))
				{
					// !!! 
					webSocketCloseStatusByClient
					    = WebSocketCloseStatus.PolicyViolation;
					webSocketCloseStatusDescriptionByClient
					    = SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_NeedApplicationIdentity;
					////////////////////////////////////////////////
					var errorDescription = "创建与服务器的连接失败，发送Api请求失败。";
					// !!!
					openConnectionError = Error.OperationFailed;
					openConnectionErrorDescription = errorDescription;
					// !!!
					throw new Exception(errorDescription);
					////////////////////////////////////////////////
				}


				var receiveBuffer
				    = new BytesBuffer(ApiResponseReceiveBufferSizeInByte);
				var clientOpenConnectResponse
				    = await socketToService.ReceiveApiResponseInfoAsync<BrClientConnectionInfoController.OpenConnectResponse>(
				    receiveBuffer,
				    //
				    _client.Config?.ServiceResponseTextEncoding ?? DataPacketTextEncoding.Default,
				    applicationAesKey,
				    //
				    clientConnectRequestPipeTimeInfo,
				    //
				    _cancellationTokenSource.Token);
				if (clientOpenConnectResponse == null)
				{
					// !!! 
					webSocketCloseStatusByClient
					    = WebSocketCloseStatus.PolicyViolation;
					webSocketCloseStatusDescriptionByClient
					    = SocketCloseStatusDescription
					    .ApplicationInfo_PolicyViolation_ConnectFailed;
					////////////////////////////////////////////////
					// !!!
					openConnectionError = Error.OperationFailed;
					openConnectionErrorDescription = "创建与服务器的连接失败，接收到的服务端数据为”空“。";
					// !!!
					throw new ApplicationException(SocketCloseStatusDescription.ApplicationInfo_PolicyViolation_ConnectFailed);
					////////////////////////////////////////////////
				}
				else if (clientOpenConnectResponse.WebSocketReceiveResult != null)
				{
					var webSocketReceiveResult
					    = clientOpenConnectResponse.WebSocketReceiveResult;
					// !!! 
					webSocketCloseStatusByService
					    = webSocketReceiveResult.CloseStatus;
					webSocketCloseStatusDescriptionByService
					    = webSocketReceiveResult.CloseStatusDescription;
					////////////////////////////////////////////////
					// !!!
					openConnectionError = Error.OperationFailed;
					openConnectionErrorDescription = "创建与服务器的连接失败，服务端关闭了连接。";
					// !!!
					////////////////////////////////////////////////
				}

				var applicationLoginResponse
				    = clientOpenConnectResponse
				    .Result;
				if (applicationLoginResponse == null)
				{
					// !!! 
					webSocketCloseStatusByClient = WebSocketCloseStatus.PolicyViolation;
					webSocketCloseStatusDescriptionByClient = SocketCloseStatusDescription
					    .ApplicationInfo_PolicyViolation_ConnectFailed;
					////////////////////////////////////////////////
					// !!!
					openConnectionError = Error.OperationFailed;
					openConnectionErrorDescription = "创建与服务器的连接失败，接收到的Api响应为空。";
					// !!!
					throw new ApplicationException(
					    SocketCloseStatusDescription
					    .ApplicationInfo_PolicyViolation_ResponseParseWithJsonFailed);
					////////////////////////////////////////////////
				}
				else
				{
					// !!!
					clientConnectionId = applicationLoginResponse.ConnectionId;
					applicationInfoConnected = applicationLoginResponse.ApplicationInfo;
					configFromService = applicationLoginResponse.ClientConfig;
					// !!!
				}

				////////////////////////////////////////////////
				// 登录失败：
				////////////////////////////////////////////////
				if (applicationLoginResponse.IsFailed)
				{
					// !!! 
					webSocketCloseStatusByClient
					    = WebSocketCloseStatus.PolicyViolation;
					webSocketCloseStatusDescriptionByClient
					    = SocketCloseStatusDescription
					    .ApplicationInfo_PolicyViolation_ConnectFailed;
					////////////////////////////////////////////////
					// !!!
					openConnectionError = applicationLoginResponse.Error;
					openConnectionErrorDescription = applicationLoginResponse.ErrorDescription;
					// !!!
					////////////////////////////////////////////////
				}
				else if (applicationInfoConnected == null
				    || !applicationInfoConnected.IsValid)
				{
					// !!! 
					webSocketCloseStatusByClient
					    = WebSocketCloseStatus.PolicyViolation;
					webSocketCloseStatusDescriptionByClient
					    = SocketCloseStatusDescription
					    .ApplicationInfo_PolicyViolation_ConnectFailed;
					////////////////////////////////////////////////
					// !!!
					openConnectionError = Error.OperationFailed;
					openConnectionErrorDescription = "创建与服务器的连接失败，接收到的应用程序信息无效。";
					// !!!
					////////////////////////////////////////////////
				}
				////////////////////////////////////////////////
				// 登录成功：
				////////////////////////////////////////////////
				else
				{
					////////////////////////////////////////////////
					// !!! ⚠
					_id = clientConnectionId;
					// !!! ⚠
					////////////////////////////////////////////////


					// !!!
					var lastApplicationInfoLogined = ApplicationInfo;
					ApplicationInfo = applicationInfoConnected;

					var lastConfigFromService = ConfigFromService;
					ConfigFromService = configFromService;

					//
					DidApplicationInfoLoginedUpdatedTo(
					    applicationInfoConnected,
					    lastApplicationInfoLogined,
					    //
					    configFromService,
					    lastConfigFromService,
					    //
					    serviceUriString);
					// !!!
					if (State == ClientState.ServiceConnecting)
					{
						lock (this)
						{
							if (State == ClientState.ServiceConnecting)
							{
								State = ClientState.ServiceConnected;
							}
						}
					}
					// !!!
				}
			}
			catch (Exception exception)
			{
				// !!!
				clientConnectException = exception;
				// !!!

				if (webSocketCloseStatusByClient == null)
				{
					if (exception is OperationCanceledException operationCanceledException
					    && operationCanceledException.CancellationToken
					    == cancelTokenToGetServiceUriByServiceDiscoverer)
					{
						// !!! 应用程序信息注册超时 !!!
						webSocketCloseStatusByClient
						    = WebSocketCloseStatus.PolicyViolation;
						webSocketCloseStatusDescriptionByClient
						    = SocketCloseStatusDescription
						    .ApplicationInfo_PolicyViolation_ConnectTimeout;
						//

						////////////////////////////////////////////////
						// !!!
						openConnectionError = Error.OperationTimeout;
						openConnectionErrorDescription = "打开服务连接失败，通过服务发现器，获取服务Uri超时。";
						// !!!
						////////////////////////////////////////////////
					}
					else
					{
						// !!! 应用程序信息注册失败，程序错误 !!!
						webSocketCloseStatusByClient
						    = WebSocketCloseStatus.InternalServerError;
						webSocketCloseStatusDescriptionByClient
						    = SocketCloseStatusDescription
						    .ApplicationInfo_PolicyViolation_ConnectFailedByProgramError;

						////////////////////////////////////////////////
						// !!!
						openConnectionError = Error.ProgramError;
						openConnectionErrorDescription = "打开服务连接失败，程序异常。";
						// !!!
						////////////////////////////////////////////////
					}
				}
			}
			finally
			{
				if (socketToService == null)
				{
					// !!!
					Interlocked.Decrement(ref _socketInfesToServiceInCreatingCount);
					// !!!
					// ⏱⏱⏱，【状态标记】
					clientConnectRequestPipeTimeInfo.State = clientConnectException != null
					    ? RequestPipeState.Exception
					    : RequestPipeState.Failed;
					// ⏱⏱⏱，【状态标记】
				}
				else
				{
					////////////////////////////////////////////////
					// 3/，处理异常导致的连接关闭信息：
					////////////////////////////////////////////////
					if (webSocketCloseStatusByService != null)
					{
						// ⚠ 如果是服务端导致的连接失败，需要将服务节点加入到不可用节点中，暂停使用。    ⚠
						// ⚠ 该节点的状态刷新，则是由服务发现器来完成：下一轮的服务发现，服务状态刷新。 ⚠
						if (serviceNodeInfoInServiceDiscoverer != null
						    && _client.ServiceDiscoverer is ServiceDiscoverer serviceDiscoverer)
						{
							serviceDiscoverer.SetServiceNodeConnectFailed(
							    serviceNodeInfoInServiceDiscoverer);
						}

						// !!!
						Interlocked.Decrement(ref _socketInfesToServiceInCreatingCount);
						// !!!
						await socketToService.TryToCloseAsync(
						    webSocketCloseStatusByService.Value,
						    webSocketCloseStatusDescriptionByService,
						    _cancellationTokenSource.Token);
						// !!!
						socketToService = null;
						// !!!

						// ⏱⏱⏱，【状态标记】
						clientConnectRequestPipeTimeInfo.State = clientConnectException != null
						    ? RequestPipeState.Exception
						    : RequestPipeState.Failed;
						// ⏱⏱⏱，【状态标记】
					}
					else if (webSocketCloseStatusByClient != null)
					{
						// !!!
						Interlocked.Decrement(ref _socketInfesToServiceInCreatingCount);
						// !!!
						await socketToService.TryToCloseAsync(
						    webSocketCloseStatusByClient.Value,
						    webSocketCloseStatusDescriptionByClient,
						    _cancellationTokenSource.Token);
						// !!!
						socketToService = null;
						// !!!

						// ⏱⏱⏱，【状态标记】
						clientConnectRequestPipeTimeInfo.State = clientConnectException != null
						    ? RequestPipeState.Exception
						    : RequestPipeState.Failed;
						// ⏱⏱⏱，【状态标记】
					}
					////////////////////////////////////////////////
					// 4/，将可用的新连接加入到连接池：
					////////////////////////////////////////////////
					else
					{
						lock (_allSocketInfesToService)
						{
							if (socketToService?.IsConnectionEnable() == true)
							{
								// !!!
								socketInfoToService = new SocketInfo(socketToService, ClientIpInfo.New);
								// !!!
								_allSocketInfesToService.Add(socketInfoToService);
								_idleSocketInfesReceiveFromService.Enqueue(socketInfoToService);
								Interlocked.Increment(ref _socketInfesToServiceInCreatedCount);
							}
							// !!!
							Interlocked.Decrement(ref _socketInfesToServiceInCreatingCount);
							// !!!
						}

						////////////////////////////////////////////////
						// 5/，开启任务，接收服务端响应：
						////////////////////////////////////////////////
						{
							TryToReceiveApiResponseInfoFromServiceAsync();
						}
					}
				}
			}
			if (socketInfoToService != null)
			{
				return socketInfoToService;
			}
		}
		catch (Exception exception)
		{
			////////////////////////////////////////////////
			// !!!
			openConnectionError = Error.ProgramError;
			openConnectionErrorDescription = "获取用于发送Api请求的Socket失败，程序异常。";
			openConnectionException = exception;
			////////////////////////////////////////////////
		}
		if (string.IsNullOrEmpty(openConnectionError))
		{
			var errorDescription = "获取用于发送Api请求的Socket失败，未知错误。";
			// !!!
			_client.BrServiceLogFile?.Logs(this, errorDescription, openConnectionException);
			// !!!
			openConnectionError = Error.UnknowError;
			openConnectionErrorDescription = errorDescription;
		}
		else
		{
			// !!!
			_client.BrServiceLogFile?.Logs(
			    this,
			    $"获取用于发送Api请求的Socket失败，错误信息为：{openConnectionError}，{StringUtil.FirstNotEmpty(openConnectionErrorDescription, "无")}。",
			    openConnectionException);
			// !!!
		}
		return new SocketInfo(new ClientWebSocket(), ClientIpInfo.New)
		{
			ErrorToOpenConnection = openConnectionError,
			ErrorDescriptionToOpenConnection = openConnectionErrorDescription
		};
	}

	protected void ReleaseSocketToService(SocketInfo? socketInfo)
	{
		if (socketInfo == null
		    || !socketInfo.IsConnectionEnable)
		{
			return;
		}

		// !!!
		_idleSocketInfesSendToService.Enqueue(socketInfo);
		// !!!
	}

	protected void TryToSendApiRequestInfoToServiceAsync()
	{
		_ = _tasksToSendApiRequest.TryRun(async () =>
		{
			await DidSendApiRequestInfoToServiceAsync();
		});
	}

	protected void TryToReceiveApiResponseInfoFromServiceAsync()
	{
		_tasksToReceiveApiResponse.TryRun(async () =>
		{
			await DidReceiveApiResponseInfoFromServiceAsync();
		});
	}

	protected void TryToProcessApiResponse()
	{
		_ = _tasksToProcessApiResponse.TryRun(async () =>
		{
			await DidProcessApiResponseAsync();
		});
	}

	public async Task<ApiResponseInfo<MethodResultType>> InvokeApiToGetApiResponseInfoAsync<MethodResultType>(
	    string? methodName,
	    object? methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams,
	    //
	    DataPackageEncryptionMethod encryptionMethod,
	    //
	    string? apiRequestIdentity = null)
	{
		if (string.IsNullOrEmpty(_client.ApplicationIdentity))
		{
			return new ApiResponseInfo<MethodResultType>()
			{
				StatusCode = (int)System.Net.HttpStatusCode.BadRequest,
				StatusDescription = "调用Api失败，无效的应用程序Id。"
			};
		}
		else if (string.IsNullOrEmpty(methodName))
		{
			return new ApiResponseInfo<MethodResultType>()
			{
				StatusCode = (int)System.Net.HttpStatusCode.BadRequest,
				StatusDescription = "调用Api失败，无效的Api方法。"
			};
		}
		////////////////////////////////////////////////
		// !!! 服务不可用时，不再开始新的请求： !!!
		////////////////////////////////////////////////
		if (IsClosing)
		{
			if (!ApiRequestIdentity.Service.Shutdown.EqualsIgnoreCase(apiRequestIdentity)
			    && !ApiRequestIdentity.Client.CloseConnection.EqualsIgnoreCase(apiRequestIdentity))
			{
				return new ApiResponseInfo<MethodResultType>()
				{
					StatusCode = (int)System.Net.HttpStatusCode.ServiceUnavailable,
					StatusDescription = "调用Api失败，服务已关机。"
				};
			}
		}

		var apiRequestInfo = new ApiRequestInfo<object>(
		    _client.ApplicationIdentity,
		    apiRequestIdentity ?? ApiRequestIdentity.New(),
		    methodName,
		    methodParams,
		    methodExtensionParams);
		var apiRequestPipeTimeInfo
		    = new ClientRequestPipeTimeInfo(RequestPipeType.ApiInvokeRequest);
		{
			// ⏱⏱⏱，【信息记录】
			AddClientRequestPipeTimeInfo(apiRequestPipeTimeInfo);
			// ⏱⏱⏱，【信息记录】
			apiRequestInfo.ClientRequestPipeTimeInfo = apiRequestPipeTimeInfo;
		}

		try
		{
			// !!! 准备数据包字节数组 !!!
			{
				// ⏱⏱⏱，【计时埋点】
				apiRequestPipeTimeInfo.SetTime_0_ToConnectService(DateTime.Now);
				apiRequestPipeTimeInfo.SetTime_1_ToSerializeRequestBytes();
				// ⏱⏱⏱，【计时埋点】

				apiRequestInfo.TryToPrepareDataPacketProperties(
				    ClientRequestTextEncoding,
				    encryptionMethod,
				    _client.ApplicationAesKey,
				    ClientRequestDataPacketSizeToCompression,
				    ClientRequestDataPacketCompressionMethodName);
			}

			// !!! 设置Api响应信息的类型： !!!
			{
				// !!!
				apiRequestInfo.ApiResponseInfoType = typeof(ApiResponseInfo<MethodResultType>);
				// !!!
			}

			var apiResponseCompletionSource = new TaskCompletionSource<ApiResponseInfoAbstract?>();
			{
				// !!!
				apiRequestInfo.ApiResponseCompletionSource = apiResponseCompletionSource;
				// !!!
			}

			// ⏱⏱⏱，【计时埋点】
			apiRequestInfo.ClientRequestPipeTimeInfo?.SetTime_2_ToAddRequestBytesToNeedSendQueue();
			// ⏱⏱⏱，【计时埋点】
			_apiRequestInfesNeedSend.Enqueue(apiRequestInfo);


			////////////////////////////////////////////////
			// !!!
			TryToSendApiRequestInfoToServiceAsync();
			// !!!
			////////////////////////////////////////////////


			////////////////////////////////////////////////
			// !!! 等待响应结束
			var apiResponseInfo = await apiResponseCompletionSource.Task;
			// !!!
			if (apiResponseInfo == null)
			{
				var statusCode = (int)System.Net.HttpStatusCode.BadRequest;
				string? statusDescription
				    = apiRequestInfo.ApiResponseError
				    ?? Error.UnknowError;
				if (apiRequestInfo.ApiResponseErrorDescription
				    is string apiResponseErrorDescription)
				{
					statusDescription += "：" + apiResponseErrorDescription;
				}
				apiResponseInfo = new ApiResponseInfo<MethodResultType>()
				{
					StatusCode = statusCode,
					StatusDescription = statusDescription
				};
			}
			////////////////////////////////////////////////
			return apiResponseInfo as ApiResponseInfo<MethodResultType>
			    ?? new()
			    {
				    StatusCode = (int)System.Net.HttpStatusCode.NotExtended,
				    StatusDescription = $"调用Api失败，返回的响应不是“{nameof(ApiResponseInfo<>)}”类型。"
			    };
		}
		catch (Exception exception)
		{
			_client.ExceptionLogFile?.Logs(this, "调用Api失败，程序异常。", exception);
			//
			apiRequestPipeTimeInfo.State = RequestPipeState.Exception;
			//
			return new ApiResponseInfo<MethodResultType>()
			{
				StatusCode = (int)System.Net.HttpStatusCode.InternalServerError,
				StatusDescription = "调用Api失败，程序异常。",
			};
		}
	}

	public async Task<ApiResponseInfo<object>> InvokeApiToGetBytesAsync(
	    string? methodName,
	    object? methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams,
	    DataPackageEncryptionMethod encryptionMethod)
	{
		var apiResponseInfo
		    = await InvokeApiToGetApiResponseInfoAsync<object>(
		    methodName,
		    methodParams,
		    methodExtensionParams,
		    encryptionMethod);
		{ }
		return apiResponseInfo;
	}

	public async Task<ApiResponseInfo<string>> InvokeApiToGetStringAsync(
	    string? methodName,
	    object? methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams,
	    DataPackageEncryptionMethod encryptionMethod)
	{
		var apiResponseInfo
		    = await InvokeApiToGetApiResponseInfoAsync<string>(
		    methodName,
		    methodParams,
		    methodExtensionParams,
		    encryptionMethod);
		{ }
		return apiResponseInfo;
	}

	public async Task<ApiResponseInfo<ObjectType>> InvokeApiToGetObjectAsync<ObjectType>(
	    string? methodName,
	    object? methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams,
	    DataPackageEncryptionMethod encryptionMethod)
	{
		var apiResponseInfo
		    = await InvokeApiToGetApiResponseInfoAsync<ObjectType>(
		    methodName,
		    methodParams,
		    methodExtensionParams,
		    encryptionMethod);
		{ }
		return apiResponseInfo;
	}

	public async Task<ApiResponseInfo<ObjectType>> InvokeApiToGetObjectAsync<ObjectType>(
	    string? methodName,
	    object? methodParams,
	    DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.None,
	    params DataPacketSection[] methodExtensionParams)
	{
		return await InvokeApiToGetObjectAsync<ObjectType>(
		    methodName,
		    methodParams,
		    // !!!
		    methodExtensionParams as IEnumerable<DataPacketSection>,
		    encryptionMethod);
	}
	#endregion


	////////////////////////////////////////////////
	// @事件节点
	////////////////////////////////////////////////

	#region 事件节点

	protected virtual void DidStateChangedFrom(
	    ClientState lastClientState,
	    ClientState clientServiceState)
	{
		if (clientServiceState == ClientState.ServiceConnected)
		{
			DidClientConnectedToService();
		}
		else if (clientServiceState == ClientState.Closed)
		{
			DidClientClosed();
		}
	}

	protected virtual void DidClientConnectedToService()
	{ }

	protected virtual void DidClientClosed()
	{
		// !!!
		// Console.WriteLine("⚠客户端，DidClientClosed，服务已关机。");
		// !!!
	}

	protected virtual void DidApplicationInfoLoginedUpdatedTo(
	    ApplicationInfo? currentApplicationInfo,
	    ApplicationInfo? lastApplicationInfo,
	    //
	    ClientConfig? currentClientConfigFromService,
	    ClientConfig? lastClientConfigFromService,
	    //
	    string serviceUriString)
	{
		var serviceName = _client.ServiceName;
		if (!serviceName.EndsWith("服务"))
		{
			serviceName += "服务";
		}
		ServiceConsoleUtil.WriteServiceInfes(
		    serviceName + "已连接至：" + serviceUriString);
	}

	protected async Task DidSendApiRequestInfoToServiceAsync()
	{
		while (true)
		{
			var socketInfoToService = await GetSocketToSendApiRequestInfoAsync(false, false);
			if (socketInfoToService?.IsConnectionEnable != true)
			{
				// 用来处理，所有的Socket连接都已经关闭的情况，但仍有请求需要发送的特殊情况，
				// 如：关机过程中，一些Socket意外中断。
				var isClientClosing = IsClosing;
				var isClientAnySocketConnectionEnable = IsAnySocketConnectionEnable;
				if (
				// 当前已关机，
				isClientClosing
				// !!! 【并且】没有可用的Socket连接时，                                             !!!
				// !!! （可用（残存）的Socket连接，是依赖数据传输结果进行关闭的） !!!
				// !!! 不再发送请求，并结束所有已有的Api请求：                                  !!!
				&& isClientAnySocketConnectionEnable == false)
				{
					while (_apiRequestInfesNeedSend.TryDequeue(out var apiRequestInfo))
					{
						if (apiRequestInfo.ClientRequestPipeTimeInfo
						is ClientRequestPipeTimeInfo clientRequestPipeTimeInfo)
						{
							// ⏱⏱⏱，【状态标记】
							clientRequestPipeTimeInfo.State
							    = RequestPipeState.Cancelled;
							// ⏱⏱⏱，【状态标记】

							// ⏱⏱⏱，【计时埋点】
							// !!! 没有真实操作，不记录时间 !!!
							// clientRequestPipeTimeInfo.SetTime_3_ToSendRequestBytesBegin();
							// clientRequestPipeTimeInfo.SetTime_4_ToSendRequestBytesEnd();
							// var now = DateTime.Now;
							// clientRequestPipeTimeInfo.SetTime_5_ToAddResponseBytesToNeedProcessQueue(now);
							// clientRequestPipeTimeInfo.SetTime_6_ToDeserializeResponseObjectBegin();
							// clientRequestPipeTimeInfo.SetTime_7_ToDeserializeResponseObjectEnd(now);
							// ⏱⏱⏱，【计时埋点】
						}
						////////////////////////////////////////////////
						if (!string.IsNullOrEmpty(socketInfoToService?.ErrorToOpenConnection))
						{
							////////////////////////////////////////////////
							// !!! 重要事件日志                                     !!!
							_client.BrServiceLogFile?.Logs(
							    this,
							    $"打开与服务端的连接失败：{socketInfoToService.ErrorToOpenConnection}，{StringUtil.FirstNotEmpty(socketInfoToService.ErrorDescriptionToOpenConnection, "无")}。",
							    null);
							////////////////////////////////////////////////
							///
							apiRequestInfo.ApiResponseError
							    = socketInfoToService.ErrorToOpenConnection;
							apiRequestInfo.ApiResponseErrorDescription
							    = socketInfoToService.ErrorDescriptionToOpenConnection;
						}
						else
						{
							apiRequestInfo.ApiResponseError
							    = Error.OperationFailed;
							apiRequestInfo.ApiResponseErrorDescription
							    = "发送Api请求信息失败，服务已关机。";
						}


						// !!!
						apiRequestInfo.ApiResponseCompletionSource
						    ?.SetResult(null);
						// !!!
						////////////////////////////////////////////////
					}
				}
				else if (isClientAnySocketConnectionEnable == false
				    && _allSocketInfesToService.Count < 1)
				{
					////////////////////////////////////////////////
					// !!! 重要事件日志                                     !!!
					_client.BrServiceLogFile?.Logs(
					    this,
					    "当前没有任何连接到服务的Socket连接，请检查当前服务的服务信息配置，如：GroupName，服务管理器端口，指定的服务Uri等配置。",
					    null);
					////////////////////////////////////////////////

					while (_apiRequestInfesNeedSend.TryDequeue(out var apiRequestInfo))
					{
						if (apiRequestInfo.ClientRequestPipeTimeInfo
						is ClientRequestPipeTimeInfo clientRequestPipeTimeInfo)
						{
							// ⏱⏱⏱，【状态标记】
							clientRequestPipeTimeInfo.State = RequestPipeState.Failed;
							// ⏱⏱⏱，【状态标记】

							// ⏱⏱⏱，【计时埋点】
							// !!! 没有真实操作，不记录时间 !!!
							// clientRequestPipeTimeInfo.SetTime_3_ToSendRequestBytesBegin();
							// clientRequestPipeTimeInfo.SetTime_4_ToSendRequestBytesEnd();
							// var now = DateTime.Now;
							// clientRequestPipeTimeInfo.SetTime_5_ToAddResponseBytesToNeedProcessQueue(now);
							// clientRequestPipeTimeInfo.SetTime_6_ToDeserializeResponseObjectBegin();
							// clientRequestPipeTimeInfo.SetTime_7_ToDeserializeResponseObjectEnd(now);
							// ⏱⏱⏱，【计时埋点】
						}
						////////////////////////////////////////////////
						if (!string.IsNullOrEmpty(socketInfoToService?.ErrorToOpenConnection))
						{
							apiRequestInfo.ApiResponseError
							    = socketInfoToService.ErrorToOpenConnection;
							apiRequestInfo.ApiResponseErrorDescription
							    = socketInfoToService.ErrorDescriptionToOpenConnection;
						}
						else
						{
							apiRequestInfo.ApiResponseError
							= Error.OperationFailed;
							apiRequestInfo.ApiResponseErrorDescription
							    = "发送Api请求信息失败，没有可用的连接。";
						}
						// !!!
						apiRequestInfo
						    .ApiResponseCompletionSource
						    ?.SetResult(null);
						// !!!
						////////////////////////////////////////////////
					}
				}
				// !!!
				return;
				// !!!
			}

			try
			{
				////////////////////////////////////////////////
				// 只有当前连接有效时，才会持续处理任务。
				////////////////////////////////////////////////
				while (socketInfoToService.IsConnectionEnable == true)
				{
					// !!! 关闭连接时，不再发送心跳包。 !!!
					if (IsClosing)
					{
						socketInfoToService.HeartbeatApiRequestInfoNeedSend = null;
					}

					ApiRequestInfo<object>? apiRequestInfo
					= socketInfoToService.HeartbeatApiRequestInfoNeedSend;
					var isApiRequestInfoHeartbeat = false;
					if (apiRequestInfo != null)
					{
						// !!!
						isApiRequestInfoHeartbeat = true;
						// !!!
					}
					else if (!_apiRequestInfesNeedSend.TryDequeue(out apiRequestInfo))
					{
						break;
					}

					if (apiRequestInfo == null
					    || string.IsNullOrEmpty(apiRequestInfo.RequestIdentity))
					{
						continue;
					}
					var apiRequestInfoDataPacket
					    = apiRequestInfo.DataPacket;
					if (apiRequestInfoDataPacket == null)
					{
						continue;
					}
					var apiRequestInfoDataPacketBytes
					    = apiRequestInfo.DataPacketBytes;
					if (apiRequestInfoDataPacketBytes == null
					    || apiRequestInfoDataPacketBytes.Value.Count < 1)
					{
						continue;
					}

					try
					{
						if (isApiRequestInfoHeartbeat)
						{
							//DebugInfo.WriteLine(
							//	"心跳包测试", "客户端",
							//	"发送心跳包，开始。");

							socketInfoToService.LastTimeOfSendHeartbeat = DateTime.Now;
						}
						else
						{
							socketInfoToService.LastTimeOfSendBusinessData = DateTime.Now;
						}
						{
							////////////////////////////////////////////////
							////////////////////////////////////////////////
							////////////////////////////////////////////////
							// !!!⚠⚠⚠ 一定要在“SendAsync”前加入队列 ⚠⚠⚠!!!
							// !!!⚠⚠⚠ "await"的下一步可能要等很久，  ⚠⚠⚠!!!
							// !!!⚠⚠⚠ 因此会出现响应已经回来，       ⚠⚠⚠!!!
							// !!!⚠⚠⚠ 但是仍然没有加入队列的情况。    ⚠⚠⚠!!!
							////////////////////////////////////////////////
							////////////////////////////////////////////////
							////////////////////////////////////////////////
							// !!! 心跳包不需要额外的响应信息： !!!
							if (!isApiRequestInfoHeartbeat)
							{
								_apiRequestInfesNeedResponse.AddOrUpdate(apiRequestInfo.RequestIdentity, apiRequestInfo,
								    (applicationIdentity, lastApiRequestInfo) =>
								    {
									    // !!!
									    throw new ApplicationException("产生了重复“RequestIdentity”的请求信息。");
									    // !!!
								    });
							}
							if (IsClosing)
							{
								DebugInfo.WriteLine(
								    "关机测试",
								    "客户端",
								    $"DidSendApiRequestInfoToServiceAsync，关机后，增加了需要响应的请求信息：{apiRequestInfo.RequestIdentity}。");
							}


							// !!!
							//Interlocked.Increment(ref _totalRequestsCountSend);
							// !!!

							// ⏱⏱⏱，【计时埋点】
							apiRequestInfo.ClientRequestPipeTimeInfo?.SetTime_3_ToSendRequestBytesBegin();
							// ⏱⏱⏱，【计时埋点】
							////////////////////////////////////////////////
							await socketInfoToService.Socket.SendAsync(
							    apiRequestInfoDataPacketBytes.Value,
							    //
							    apiRequestInfoDataPacket.WebSocketMessageType,
							    true,
							    //
							    _cancellationTokenSource.Token);
							////////////////////////////////////////////////
							// ⏱⏱⏱，【计时埋点】
							apiRequestInfo.ClientRequestPipeTimeInfo?.SetTime_4_ToSendRequestBytesEnd();
							// ⏱⏱⏱，【计时埋点】
						}
						if (isApiRequestInfoHeartbeat)
						{
							//DebugInfo.WriteLine(
							//	"心跳包测试", "客户端",
							//	"发送心跳包，结束。");

							// !!!
							socketInfoToService.HeartbeatApiRequestInfoNeedSend = null;
							// !!!
							socketInfoToService.LastTimeOfSendHeartbeat = DateTime.Now;
						}
						else
						{
							socketInfoToService.LastTimeOfSendBusinessData = DateTime.Now;
						}
					}
					catch (Exception exception)
					{
						_client.ExceptionLogFile?.Logs(this, "调用Api失败，程序异常。", exception);

						if (apiRequestInfo.ClientRequestPipeTimeInfo
						is ClientRequestPipeTimeInfo clientRequestPipeTimeInfo)
						{
							// ⏱⏱⏱，【状态标记】
							clientRequestPipeTimeInfo.State = RequestPipeState.Exception;
							// ⏱⏱⏱，【状态标记】
						}
					}
				}
			}
			catch (Exception exception)
			{
				_client.ExceptionLogFile?.Logs(this, "获取待调用的Api信息失败，程序异常。", exception);
			}
			finally
			{
				ReleaseSocketToService(socketInfoToService);
			}

			if (_apiRequestInfesNeedSend.IsEmpty()
			&& IsAnySocketNeedSendHeartbeta != true)
			{
				// !!!
				break;
				// !!!
			}
		}
	}

	protected async Task DidReceiveApiResponseInfoFromServiceAsync()
	{
		var receiveBuffer = new BytesBuffer(ApiResponseReceiveBufferSizeInByte);
		while (_idleSocketInfesReceiveFromService.TryDequeue(out var socketInfoReceiveFromService))
		{
			WebSocketReceiveResult? socketToServiceReceiveResult = null;
			try
			{
				if (socketInfoReceiveFromService == null
				    // 当前网络连接不可用的，不再属于空闲网络连接。
				    || socketInfoReceiveFromService.IsConnectionEnable != true)
				{
					DebugInfo.WriteLine(
					    "关机测试",
					    "客户端",
					    "DidReceiveApiResponseInfoFromServiceAsync，获取到了已断开连接的Socket。");
					continue;
				}

				var serviceResponseTextEncoding
				    = _client.Config?.ServiceResponseTextEncoding
				    ?? DataPacketTextEncoding.Default;
				await foreach (
				    var dataPacketBytesResult
				    in socketInfoReceiveFromService.Socket.ReceiveByteArraysAsync(
				    receiveBuffer,
				    _cancellationTokenSource.Token))
				{
					var timeOfReceiveData = DateTime.Now;
					if (dataPacketBytesResult == null)
					{
						continue;
					}

					////////////////////////////////////////////////
					////////////////////////////////////////////////
					// !!!
					socketToServiceReceiveResult = dataPacketBytesResult.Result;
					if (socketToServiceReceiveResult?.CloseStatus != null)
					{
						DebugInfo.WriteLine(
						    "关机测试",
						    "客户端",
						    $"DidReceiveApiResponseInfoFromServiceAsync，已收到Socket的关闭状态：{socketToServiceReceiveResult.CloseStatus.Name()}。");
						// !!!
						break;
						// !!!
					}
					// !!!
					////////////////////////////////////////////////
					////////////////////////////////////////////////

					var dataPacketBytes = dataPacketBytesResult.BytesReceived;
					if (dataPacketBytes == null
					    || dataPacketBytes.Length < 1)
					{
						DebugInfo.WriteLine(
						    "关机测试",
						    "客户端",
						    "DidReceiveApiResponseInfoFromServiceAsync，接收响应为空。");
						continue;
					}

					//Interlocked.Increment(ref _totalRequestsCountReceived);

					_apiResponseInfesNeedProcess.Enqueue(
					    new ApiResponseInfoBytesInfo(
					    socketInfoReceiveFromService,
					    dataPacketBytes,
					    serviceResponseTextEncoding,
					    //
					    timeOfReceiveData));

					////////////////////////////////////////////////
					// !!!
					TryToProcessApiResponse();
					// !!!
					////////////////////////////////////////////////
				}
				DebugInfo.WriteLine(
				    "关机测试",
				    "客户端",
				    "DidReceiveApiResponseInfoFromServiceAsync，已跳出接收循环，不再接收响应。");
			}
			catch (Exception exception)
			{
				_client.ExceptionLogFile?.Logs(this, "接收服务响应信息失败，程序异常。", exception);
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
					if (socketToServiceReceiveResult?.CloseStatus != null)
					{
						closeStatus = socketToServiceReceiveResult.CloseStatus.Value;
						closeStatusDescription = socketToServiceReceiveResult.CloseStatusDescription;
					}
					else
					{
						closeStatus = WebSocketCloseStatus.EndpointUnavailable;
						closeStatusDescription = SocketCloseStatusDescription.ClientInfo_EndpointUnavailable_UnexpectedCloseOperation;
					}

					// !!!
					Interlocked.Increment(ref _socketInfesToServiceInClosedCount);
					//
					await socketInfoReceiveFromService.Socket.TryToCloseAsync(
					    closeStatus,
					    closeStatusDescription,
					    _cancellationTokenSource.Token);
					// !!!
					lock (_allSocketInfesToService)
					{
						_allSocketInfesToService.Remove(socketInfoReceiveFromService);
					}
					// !!!

					DebugInfo.WriteLine(
					    "关机测试",
					    "客户端",
					    "DidReceiveApiResponseInfoFromServiceAsync，已关闭Socket连接。");
				}
				catch (Exception exception)
				{
					_client.ExceptionLogFile?.Logs(this, "关闭Socket连接失败，程序异常。", exception);
				}
			}
		}
	}

	protected async Task DidProcessApiResponseAsync()
	{
		while (_apiResponseInfesNeedProcess.TryDequeue(out var apiResponseInfoBytesInfo))
		{
			ClientRequestPipeTimeInfo? clientRequestPipeTimeInfo = null;
			try
			{
				var dataPacket
				= DataPacket.CreateDataPacketWithBytes(
				    apiResponseInfoBytesInfo.Bytes,
				    apiResponseInfoBytesInfo.TextEncoding,
				    _client.ApplicationAesKey);
				if (dataPacket == null)
				{
					continue;
				}
				var requestIdentity = dataPacket.GetHeaderParam(DataPacketHeaderName.RequestIdentity);
				if (string.IsNullOrEmpty(requestIdentity))
				{
					continue;
				}

				// !!! 【内置特殊API】：         !!!
				// !!! 心跳包，直接忽略即可。 !!!
				if (ApiRequestIdentity.Service.Heartbeat.EqualsIgnoreCase(requestIdentity))
				{
					//DebugInfo.WriteLine(
					//				"心跳包测试", "客户端",
					//				"收到心跳包。");

					apiResponseInfoBytesInfo.SocketInfo.LastTimeOfReceiveHeartbeat
					= apiResponseInfoBytesInfo.TimeOfReceiveData;
					// !!! 特殊命令可能是由Api渠道调用的，因此不能“continue” !!!
					// continue;
				}


				////////////////////////////////////////////////
				////////////////////////////////////////////////
				////////////////////////////////////////////////
				// !!! 没有找到发起信息，则尝试触发服务端下发消息事件。!!!
				//Interlocked.Increment(ref _totalRequestsCountTryRemoveFromQueue);
				if (!_apiRequestInfesNeedResponse.TryRemove(requestIdentity, out var apiRequestInfo))
				{
					// !!! 如果服务端下发的消息也不能触发，则直接忽略。 !!!
					if (!_client.TryGetApiRequestInfoNeedResponseOriginateService(
					    requestIdentity,
					    out apiRequestInfo))
					{
						continue;
					}
				}
				//Interlocked.Increment(ref _totalRequestsCountRemoveFromQueue);
				////////////////////////////////////////////////
				////////////////////////////////////////////////
				////////////////////////////////////////////////

				if (apiRequestInfo?.ApiResponseInfoType == null)
				{
					continue;
				}

				// !!!
				clientRequestPipeTimeInfo = apiRequestInfo.ClientRequestPipeTimeInfo;
				// !!!
				if (clientRequestPipeTimeInfo != null)
				{
					// ⏱⏱⏱，【计时埋点】
					clientRequestPipeTimeInfo.SetTime_6_ToAddResponseBytesToNeedProcessQueue(
					    apiResponseInfoBytesInfo.TimeOfReceiveData);
					clientRequestPipeTimeInfo.SetTime_7_ToDeserializeResponseObjectBegin();
					// ⏱⏱⏱，【计时埋点】

				}

				ApiResponseInfoAbstract? apiResponseInfo = null;
				try
				{
					apiResponseInfo = dataPacket.ToApiResponseInfo(apiRequestInfo.ApiResponseInfoType);
				}
				catch (Exception exception)
				{
					apiResponseInfo = Activator.CreateInstance(apiRequestInfo.ApiResponseInfoType) as ApiResponseInfoAbstract;
					if (apiResponseInfo != null)
					{
						apiResponseInfo.StatusCode = (int)HttpStatusCode.InternalServerError;
						apiResponseInfo.StatusDescription = "解析服务端响应时出错，程序异常：\r\n" + exception.Message;
					}
				}
				if (apiResponseInfo != null)
				{
					apiResponseInfoBytesInfo.SocketInfo.LastTimeOfReceiveBusinessData = apiResponseInfoBytesInfo.TimeOfReceiveData;
				}


				// ⏱⏱⏱，【计时埋点】
				apiRequestInfo.ClientRequestPipeTimeInfo
				?.SetTime_8_ToDeserializeResponseObjectEnd(DateTime.Now);
				// ⏱⏱⏱，【计时埋点】


				////////////////////////////////////////////////
				////////////////////////////////////////////////
				////////////////////////////////////////////////
				// !!!
				//Interlocked.Increment(ref _totalRequestsCountSetResult);
				var apiRequestInfoApiResponseCompletionSource
				    = apiRequestInfo.ApiResponseCompletionSource;
				var apiRequestInfoToReceiveServiceOriginalApiResponseAsync
				    = apiRequestInfo.ToReceiveServiceOriginalApiResponseAsync;
				if (apiRequestInfoApiResponseCompletionSource != null)
				{
					apiRequestInfoApiResponseCompletionSource.SetResult(apiResponseInfo);
				}
				else if (apiRequestInfoToReceiveServiceOriginalApiResponseAsync != null)
				{
					await apiRequestInfoToReceiveServiceOriginalApiResponseAsync(apiResponseInfo);
				}
				// !!!
				////////////////////////////////////////////////
				////////////////////////////////////////////////
				////////////////////////////////////////////////

				// !!!
				DidApiResponseInfoProcessed(apiResponseInfo);
				// !!!
			}
			catch (Exception exception)
			{
				_client.ExceptionLogFile?.Logs(this, "处理API响应失败，程序异常。", exception);

				// ⏱⏱⏱，【状态标记】
				clientRequestPipeTimeInfo?.State = RequestPipeState.Exception;
			}
		}

		if (// 客户端已关机：
		IsClosing
		// 客户端已空闲，可以进行关闭操作：
		&& IsClientIdle)
		{
			// 【关闭连接的Api】尚未调用，则调用，
			// 用于在服务端标记该连接需要关闭，不要再使用该连接向客户端传输数据。
			if (_isCloseConnectionApiInvoked == false)
			{
				lock (this)
				{
					if (_isCloseConnectionApiInvoked == false)
					{
						_isCloseConnectionApiInvoked = true;

						// !!! 调用关闭连接的Api，是在告诉服务端：  !!!
						// !!! 本连接将要关闭，                                   !!!
						// !!! 不要在使用本连接，向客户端发送数据。 !!!
						_ = InvokeApiToGetApiResponseInfoAsync<object>(
						    ServiceMethodNames.CloseConnection,
						    null,
						    null,
						    DataPackageEncryptionMethod.None,
						    ApiRequestIdentity.Client.CloseConnection);

						DebugInfo.WriteLine(
						    "关机测试",
						    "客户端",
						    "_isCloseConnectionApiResponsed，已请求服务端的关闭连接。");
					}
				}
			}
			// 【关闭连接的Api】已经由服务端确认，则此时可以安全的关闭连接了。
			else if (_isCloseConnectionApiResponsed)
			{
				////////////////////////////////////////////////
				// ⚠️ 关闭所有Socket连接： ⚠
				////////////////////////////////////////////////

				var closeStatus = WebSocketCloseStatus.NormalClosure;
				var statusDescription = SocketCloseStatusDescription.ClientInfo_NormalClosure_CloseConnection;
				var allSocketInfesToServiceCount = _allSocketInfesToService.Count;
				DebugInfo.WriteLine(
				    "关机测试",
				    "客户端",
				    $"DidProcessApiResponse，关闭所有Socket开始，总共 {allSocketInfesToServiceCount} 个连接需要关闭。");
				{
					var tasksToCloseSocketToService = new List<Task>();
					for (var socketInfoToService = await GetSocketToSendApiRequestInfoAsync(false, true);
					    socketInfoToService?.IsConnectionEnable == true;
					    socketInfoToService = await GetSocketToSendApiRequestInfoAsync(false, true))
					{
						var currentSocketInfoToService = socketInfoToService;
						tasksToCloseSocketToService.Add(Task.Run(async () =>
						{
							try
							{
								DebugInfo.WriteLine(
					    "关机测试",
					    "客户端",
					    $"DidProcessApiResponse，准备关闭Socket，Socket状态为：{currentSocketInfoToService.Socket.State}。");

								await currentSocketInfoToService.Socket.TryToCloseAsync(
					    closeStatus,
					    statusDescription,
					    _cancellationTokenSource.Token);

								DebugInfo.WriteLine(
					    "关机测试",
					    "客户端",
					    $"DidProcessApiResponse，已关闭Socket，Socket状态为：{currentSocketInfoToService.Socket.State}。");
							}
							catch (Exception exception)
							{
								_client.ExceptionLogFile?.Logs(this, "关闭连接服务端的Socket失败，程序异常。", exception);
							}
						}));
					}
					// !!! ⚠
					lock (_allSocketInfesToService)
					{
						_allSocketInfesToService.Clear();
					}
					_idleSocketInfesSendToService.Clear();
					_idleSocketInfesReceiveFromService.Clear();
					// !!! ⚠
					// !!!
					Task.WaitAll([.. tasksToCloseSocketToService]);
					// !!!
				}
				DebugInfo.WriteLine(
				    "关机测试",
				    "客户端",
				    $"DidProcessApiResponse，关闭所有Socket结束，总共 {allSocketInfesToServiceCount} 个连接被关闭。");
			}
		}
	}

	protected void DidApiResponseInfoProcessed(ApiResponseInfoAbstract? apiResponseInfo)
	{
		if (apiResponseInfo == null)
		{
			return;
		}

		// !!! 客户端发起的关机请求，得到了响应，!!!
		// !!! 从此服务端不再使用该连接发送数据。 !!!
		if (ApiRequestIdentity.Client.CloseConnection.EqualsIgnoreCase(apiResponseInfo.RequestIdentity))
		{
			if (_isCloseConnectionApiResponsed)
			{
				throw new Exception($"客户端关机信息异常，收到了重复的客户端关机响应：{ApiRequestIdentity.Client.CloseConnection}。");
			}
			// !!!!
			_isCloseConnectionApiResponsed = true;
			// !!!!

			DebugInfo.WriteLine(
			    "关机测试",
			    "客户端",
			    "_isCloseConnectionApiResponsed，已收到服务端的关闭连接确认。");
		}
		// !!! 服务端发起的关机请求，需要客户端开始进行关机操作。 !!!
		else if (ApiRequestIdentity.Client.CloseConnection.EqualsIgnoreCase(apiResponseInfo.RequestIdentity))
		{
			// !!!⚠ 开始关机： ⚠!!!
			_ = CloseAsync();
		}
	}

	protected void DidQueueHeartbeatRequestInfoToService(
	    CancellationToken cancellationToken)
	{
		// !!! 关闭连接时，不再发送心跳包。 !!!
		if (IsClosing)
		{
			return;
		}
		var applicationIdentity = _client.ApplicationIdentity;
		if (string.IsNullOrEmpty(applicationIdentity))
		{
			return;
		}

		SocketInfo[] socketInfesToService;
		lock (_allSocketInfesToService)
		{
			socketInfesToService = [.. _allSocketInfesToService];
		}

		if (socketInfesToService == null
		    || socketInfesToService.Length < 1)
		{
			return;
		}

		//DebugInfo.WriteLine(
		//	"心跳包测试", "客户端",
		//	"尝试检查并安排心跳包任务。");

		var now = DateTime.Now;
		var socketConnectionHeartbeatIntervalSeconds
		    = _client.Config?.SocketConnectionHeartbeatIntervalSecondsFinally
		    ?? TimerIntervalSeconds.SocketConnectionHeartbeatIntervalSecondsMin;
		var heartbeatTasksCount = 0;
		foreach (var socketInfo in socketInfesToService)
		{
			if (cancellationToken.IsCancellationRequested
			    || IsClosing)
			{
				return;
			}

			if (// !!!! 不重复设置心跳包，否则可能产生心跳包的需求积压。 !!!
			socketInfo.HeartbeatApiRequestInfoNeedSend == null
			&& socketInfo.IsNeedSendHeartbeatAtTime(
			    now,
			    socketConnectionHeartbeatIntervalSeconds))
			{
				var heartbeatApiRequestInfo
				= new ApiRequestInfo<object>(
				    applicationIdentity,
				    ApiRequestIdentity.Client.Heartbeat,
				    ServiceMethodNames.Heartbeat,
				    null,
				    null);
				var clientRequestPipeTimeInfo
				    = new ClientRequestPipeTimeInfo(RequestPipeType.HeartbeatRequestFromCient);
				{
					heartbeatApiRequestInfo.ClientRequestPipeTimeInfo = clientRequestPipeTimeInfo;
					// ⏱⏱⏱，【信息记录】
					AddClientRequestPipeTimeInfo(clientRequestPipeTimeInfo);
					// ⏱⏱⏱，【信息记录】
				}
				{
					// ⏱⏱⏱，【计时埋点】
					clientRequestPipeTimeInfo.SetTime_0_ToConnectService(DateTime.Now);
					clientRequestPipeTimeInfo.SetTime_1_ToSerializeRequestBytes();
					// ⏱⏱⏱，【计时埋点】

					heartbeatApiRequestInfo.TryToPrepareDataPacketProperties(
					    ClientRequestTextEncoding,
					    DataPackageEncryptionMethod.None,
					    _client.ApplicationAesKey,
					    ClientRequestDataPacketSizeToCompression,
					    ClientRequestDataPacketCompressionMethodName);
				}
				// ⏱⏱⏱，【计时埋点】
				clientRequestPipeTimeInfo.SetTime_2_ToAddRequestBytesToNeedSendQueue();
				// ⏱⏱⏱，【计时埋点】
				// !!!
				socketInfo.HeartbeatApiRequestInfoNeedSend
				    = heartbeatApiRequestInfo;
				// !!!
				heartbeatTasksCount++;
			}
		}
		//DebugInfo.WriteLine(
		//	"心跳包测试", "客户端",
		//	$"尝试检查并安排心跳包任务，共安排{heartbeatTasksCount}/{_allSocketInfesToService.Length}个任务。");


		////////////////////////////////////////////////
		// !!!
		TryToSendApiRequestInfoToServiceAsync();
		// !!!
		////////////////////////////////////////////////
	}

	#endregion


	////////////////////////////////////////////////
	// @实现“IDispose”
	////////////////////////////////////////////////

	#region 实现“IDispose”

	public void Dispose()
	{
		_cancellationTokenSource.Cancel();
		while (_idleSocketInfesSendToService.TryDequeue(out var socketToService))
		{
			socketToService?.Dispose();
		}
		lock (_allSocketInfesToService)
		{
			_allSocketInfesToService.Clear();
		}

		////////////////////////////////////////////////
		GC.SuppressFinalize(this);
		////////////////////////////////////////////////
	}

	#endregion
}