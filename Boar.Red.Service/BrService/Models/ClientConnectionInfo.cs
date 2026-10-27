using BaoXia.Utils.ConcurrentTools;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Models;
using Boar.Red.Service.BrService.Constants;
using Boar.Red.Service.BrService.Extensions;
using Boar.Red.Service.BrService.Middlewares;
using Boar.Red.Service.BrService.Models.StatisticsInfo;
using Boar.Red.Service.Utils;
using Boar.Red.Service.WebService.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;

namespace Boar.Red.Service.BrService.Models;

public class ClientConnectionInfo
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public int Id { get; set; }

	public ApplicationInfo ApplicationInfo { get; set; }


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

	protected bool _isAllSocketCloseStarted = false;

	public DateTime ConnectTime { get; set; }

	public DateTime LastInvokeTime { get; set; }

	public DateTime DisconnectTime { get; set; }


	protected SocketInfo[] _socketInfes = [];

	protected object _socketInfesLocker = new();

	public int SocketInfesCount
	{
		get
		{
			return _socketInfes.Length;
		}
	}

	protected ConcurrentQueue<SocketInfo> _idleSocketInfesForSendToClient = new();

	public bool IsAnySocketConnected
	{
		get
		{
			foreach (var idleSocketInfo in _idleSocketInfesForSendToClient)
			{
				if (idleSocketInfo.IsConnectionEnable)
				{
					// !!!
					return true;
					// !!!
				}
			}
			return false;
		}
	}

	protected bool IsAnySocketNeedSendHeartbeta
	{
		get
		{
			var isAnySocketNeedSendHeartbeta = false;
			foreach (var idleSocketInfo in _idleSocketInfesForSendToClient)
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


	protected ConcurrentQueue<ApiResponseInfoAbstract> _responseInfesNeedSend = new();


	public ConcurrentQueue<ApiResponseInfoAbstract> ResponseInfesNeedSend => _responseInfesNeedSend;


	protected Tasks _tasksToSendResponseInfes;


	public CancellationTokenSource CancellationTokenSource { get; set; } = new();

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ClientConnectionInfo(
	    int id,
	    ApplicationInfo applicationInfo)
	{
		Id = id;

		_tasksToSendResponseInfes = new(
		    () =>
		    {
			    var webSocketsCount = _socketInfes.Length;
			    if (webSocketsCount < 1)
			    {
				    webSocketsCount = 1;
			    }
			    return webSocketsCount;
		    });
		//
		ApplicationInfo = applicationInfo;
		//
	}

	public bool TryAddSocket(
	    WebSocket webSocket,
	    ClientIpInfo clientIpInfo,
	    [NotNullWhen(true)]
    out SocketInfo? applicationWebSocketInfo)
	{
		//
		applicationWebSocketInfo = null;
		//

		lock (_socketInfesLocker)
		{
			var connectionsCountMax = ApplicationInfo?.ConnectionsCountMax ?? 0;
			if (connectionsCountMax <= 0
			    || _socketInfes.Length < connectionsCountMax)
			{
				applicationWebSocketInfo = new SocketInfo(webSocket, clientIpInfo);
				{
					_socketInfes = _socketInfes.ArrayByAdd(applicationWebSocketInfo);
					_idleSocketInfesForSendToClient.Enqueue(applicationWebSocketInfo);
				}
				return true;
			}
		}
		return false;
	}

	public void RemoveSocket(WebSocket webSocket)
	{
		lock (_socketInfesLocker)
		{
			var webSocketInfoIndex = IndexOfSocket(webSocket);
			if (webSocketInfoIndex >= 0)
			{
				_socketInfes = _socketInfes.ArrayByRemoveAt(webSocketInfoIndex);
			}
		}
	}

	public int IndexOfSocket(WebSocket webSocket)
	{
		var webSocketInfoIndex = -1;
		var socketInfes = _socketInfes;
		for (var socketInfoIndex = 0;
		    socketInfoIndex < socketInfes.Length;
		    socketInfoIndex++)
		{
			var socketInfo = socketInfes[socketInfoIndex];
			if (socketInfo.Socket == webSocket)
			{
				webSocketInfoIndex = socketInfoIndex;
				break;
			}
		}
		return webSocketInfoIndex;
	}

	public void TrySendResponseInfesToClient(
	    BrServiceMiddleware service,
	    ServiceConfig serviceConfig)
	{
		_ = _tasksToSendResponseInfes.TryRun(async () =>
		{
			await DidSendResponseInfesToClientInServiceAsync(
		service,
		serviceConfig);
		});
	}

	protected async Task<bool> AddApiResponseInfoToSendToClientAsync(
	    ApiResponseInfoAbstract? apiResponseInfo,
	    BrServiceMiddleware service,
	    ServiceConfig serviceConfig,
	    DataPackageEncryptionMethod dataEncryptionMethod,
	    string applicationAesKey,
	    bool isNeedWaitSentResult)
	{
		if (apiResponseInfo == null)
		{
			return false;
		}

		// !!! ⚠ 准备数据包属性 ⚠!!!
		// ⏱⏱⏱，【计时埋点】
		apiResponseInfo.ServiceRequestPipeTimeInfo?.SetTime_4_ToSerializeResponseObject();
		// ⏱⏱⏱，【计时埋点】
		if (!apiResponseInfo.TryToPrepareDataPacketProperties(
		    serviceConfig.ServiceResponseTextEncoding,
		    dataEncryptionMethod,
		    applicationAesKey,
		    serviceConfig.ServiceResponseDataPacketSizeToCompression,
		    serviceConfig.ServiceResponseDataPacketCompressionMethodName))
		{
			return false;
		}

		////////////////////////////////////////////////
		{
			// ⏱⏱⏱，【计时埋点】
			apiResponseInfo.ServiceRequestPipeTimeInfo?.SetTime_5_ToAddResponseBytesToNeedSendQueue();
			// ⏱⏱⏱，【计时埋点】
			_responseInfesNeedSend.Enqueue(apiResponseInfo);
		}
		////////////////////////////////////////////////

		// !!!
		TrySendResponseInfesToClient(service, serviceConfig);
		// !!!

		if (isNeedWaitSentResult)
		{
			var apiResponseSentCompletionSource
			    = apiResponseInfo.ApiResponseSentCompletionSource;
			if (apiResponseSentCompletionSource != null)
			{
				var apiResponseSentResult = await apiResponseSentCompletionSource.Task;
				{ }
				return apiResponseSentResult;
			}
			return false;
		}
		return true;
	}

	public void AddApiResponseInfoToSendToClient(
	    ApiResponseInfoAbstract? apiResponseInfo,
	    BrServiceMiddleware service,
	    ServiceConfig serviceConfig,
	    DataPackageEncryptionMethod dataEncryptionMethod,
	    string applicationAesKey)
	{
		_ = AddApiResponseInfoToSendToClientAsync(
		    apiResponseInfo,
		    service,
		    serviceConfig,
		    dataEncryptionMethod,
		    applicationAesKey,
		    false);
	}

	public async Task<bool> SendApiResponseInfoToSendToClientAsync(
	    ApiResponseInfoAbstract? apiResponseInfo,
	    BrServiceMiddleware service,
	    ServiceConfig serviceConfig,
	    DataPackageEncryptionMethod dataEncryptionMethod,
	    string applicationAesKey)
	{
		return await AddApiResponseInfoToSendToClientAsync(
		    apiResponseInfo,
		    service,
		    serviceConfig,
		    dataEncryptionMethod,
		    applicationAesKey,
		    true);
	}

	public void TryAddHeartbeatApiResponseInfoToSendToClient(
	    BrServiceMiddleware service,
	    ServiceConfig serviceConfig)
	{
		// !!! 关闭连接时，不再发送心跳包。 !!!
		if (IsClosing)
		{
			return;
		}

		var socketInfes = _socketInfes;
		if (socketInfes == null
		    || socketInfes.Length < 1)
		{
			return;
		}

		var now = DateTime.Now;
		var socketConnectionHeartbeatIntervalSeconds
		    = serviceConfig.SocketConnectionHeartbeatIntervalSecondsFinally;
		foreach (var socketInfo in socketInfes)
		{
			if (// !!!! 不重复设置心跳包，否则可能产生心跳包的需求积压。 !!!
			    socketInfo.HeartbeatApiResponseInfoNeedSend == null
			    && socketInfo.IsNeedSendHeartbeatAtTime(
			    now,
			    socketConnectionHeartbeatIntervalSeconds))
			{
				var serviceRequestPipeTimeInfo
				    = new ServiceRequestPipeTimeInfo(RequestPipeType.HeartbeatRequestFromServer);
				{
					// ⏱⏱⏱，【计时埋点】
					serviceRequestPipeTimeInfo.SetTime_0_ToAddRequestBytesToNeedProcessQueue(DateTime.Now);
					serviceRequestPipeTimeInfo.SetTime_1_ToDeserializeRequestBytes();
					serviceRequestPipeTimeInfo.SetTime_2_ToRouteRequest();
					serviceRequestPipeTimeInfo.SetTime_3_ToCreateResponseObject();
					// ⏱⏱⏱，【计时埋点】
				}

				var heartbeatApiResponseInfo
				= new ApiResponseInfo<object>(ApiRequestIdentity.Service.Heartbeat)
				{
					StatusCode = (int)System.Net.HttpStatusCode.OK
				};
				{
					heartbeatApiResponseInfo.ServiceRequestPipeTimeInfo = serviceRequestPipeTimeInfo;
					// ⏱⏱⏱，【信息记录】
					service.AddServiceRequestPipeTimeInfo(serviceRequestPipeTimeInfo);
					// ⏱⏱⏱，【信息记录】

					// ⏱⏱⏱，【计时埋点】
					serviceRequestPipeTimeInfo.SetTime_4_ToSerializeResponseObject();
					// ⏱⏱⏱，【计时埋点】
					heartbeatApiResponseInfo.TryToPrepareDataPacketProperties(
					    serviceConfig.ServiceResponseTextEncoding,
					    DataPackageEncryptionMethod.None,
					    ApplicationInfo.AesKey,
					    serviceConfig.ServiceResponseDataPacketSizeToCompression,
					    serviceConfig.ServiceResponseDataPacketCompressionMethodName);
				}
				// ⏱⏱⏱，【计时埋点】
				serviceRequestPipeTimeInfo.SetTime_5_ToAddResponseBytesToNeedSendQueue();
				// ⏱⏱⏱，【计时埋点】
				// !!!
				socketInfo.HeartbeatApiResponseInfoNeedSend = heartbeatApiResponseInfo;
				// !!!
			}
		}


		////////////////////////////////////////////////
		// !!!
		TrySendResponseInfesToClient(service, serviceConfig);
		// !!!
		////////////////////////////////////////////////
	}

	public void SetClose()
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
			DebugInfo.WriteLine("关机测试", "客户端", "SetClose，开始关闭连接。");
			// !!!
		}
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
			DidServiceConnected();
		}
		else if (clientServiceState == ClientState.Closed)
		{
			DidServiceShutdown();
		}
	}

	protected static void DidServiceConnected()
	{
		// !!!
		Console.WriteLine("⚠客户端连接，DidServiceConnected，服务已连接。");
		// !!!
	}

	protected static void DidServiceShutdown()
	{
		// !!!
		Console.WriteLine("⚠客户端连接，DidClientClosed，服务已关机。");
		// !!!
	}

	protected async Task DidSendResponseInfesToClientInServiceAsync(
	    BrServiceMiddleware service,
	    ServiceConfig serviceConfig)
	{
		while (_idleSocketInfesForSendToClient.TryDequeue(out var socketInfo))
		{
			var webSocket = socketInfo.Socket;
			try
			{
				if (!webSocket.IsConnectionEnable())
				{
					continue;
				}

				while (true)
				{
					// !!! 关闭连接时，不再发送心跳包。 !!!
					if (IsClosing)
					{
						socketInfo.HeartbeatApiResponseInfoNeedSend = null;
					}
					var apiResponseInfo = socketInfo.HeartbeatApiResponseInfoNeedSend;
					var isApiResponseInfoHeartbeat = false;
					if (apiResponseInfo != null)
					{
						// !!!
						isApiResponseInfoHeartbeat = true;
						// !!!

						if (apiResponseInfo.ServiceRequestPipeTimeInfo
						is ServiceRequestPipeTimeInfo serviceRequestPipeTimeInfo)
						{
							// ⏱⏱⏱，【类型标记】
							serviceRequestPipeTimeInfo.Type = RequestPipeType.HeartbeatRequestFromServer;
							// ⏱⏱⏱，【类型标记】
						}
					}
					else if (!_responseInfesNeedSend.TryDequeue(out apiResponseInfo))
					{
						break;
					}


					var dataPacket = apiResponseInfo.DataPacket;
					if (dataPacket == null)
					{
						continue;
					}
					var dataPacketBytes = apiResponseInfo.DataPacketBytes;
					if (dataPacketBytes == null
					|| dataPacketBytes.Length < 1)
					{
						continue;
					}

					try
					{
						if (isApiResponseInfoHeartbeat)
						{
							//DebugInfo.WriteLine(
							//	"心跳包测试", "服务端",
							//	"发送心跳包，开始。");
							socketInfo.LastTimeOfSendHeartbeat = DateTime.Now;
						}
						else
						{
							socketInfo.LastTimeOfSendBusinessData = DateTime.Now;
						}
						// ⏱⏱⏱，【计时埋点】
						apiResponseInfo.ServiceRequestPipeTimeInfo?.SetTime_6_ToSendResponseBytesBegin();
						// ⏱⏱⏱，【计时埋点】
						////////////////////////////////////////////////
						await webSocket.SendWithTimeoutAsync(dataPacketBytes, dataPacket.WebSocketMessageType, true,
							service.WebServiceConfig.WebSocketSendTimeoutSeconds, CancellationTokenSource.Token);
						////////////////////////////////////////////////
						// ⏱⏱⏱，【计时埋点】
						var now = DateTime.Now;
						apiResponseInfo.ServiceRequestPipeTimeInfo?.SetTime_7_ToSendResponseBytesEnd(now);
						// ⏱⏱⏱，【计时埋点】
						if (isApiResponseInfoHeartbeat)
						{
							//DebugInfo.WriteLine(
							//	"心跳包测试", "服务端",
							//	"发送心跳包，开始。");
							socketInfo.LastTimeOfSendHeartbeat = now;
						}
						else
						{
							socketInfo.LastTimeOfSendBusinessData = now;
						}

						//Interlocked.Increment(ref BrServiceMiddleware._totalResponsesCountSend);

						////////////////////////////////////////////////
						// !!! 服务的事件节点。 !!!
						service.DidApiResponseSent(apiResponseInfo, null);
						// !!!
						////////////////////////////////////////////////

					}
					catch (Exception exception)
					{
						service
						    .ExceptionLogFile
						    ?.Logs(this, "发送服务响应至客户端失败，程序异常。", exception);
						//
						switch (serviceConfig.ResponseSendExceptionAction)
						{
							default:
							case ApiResponseSendExceptionAction.Ignore:
								{
									////////////////////////////////////////////////
									// !!! 服务的事件节点。 !!!
									service.DidApiResponseSent(apiResponseInfo, exception);
									// !!!
									////////////////////////////////////////////////


									if (apiResponseInfo.ServiceRequestPipeTimeInfo
									is ServiceRequestPipeTimeInfo serviceRequestPipeTimeInfo)
									{
										// ⏱⏱⏱，【状态标记】
										serviceRequestPipeTimeInfo.State = RequestPipeState.Exception;
										// ⏱⏱⏱，【状态标记】
									}
								}
								break;
								//case ApiResponseSendExceptionAction.Resend:
								//        {
								//                // !!!
								//                _responseInfesNeedSend.Enqueue(apiResponseInfo);
								//                // !!!
								//        }
								//        break;
						}

						////////////////////////////////////////////////
						// !!! 发生异常时，要结束处理任务队列 !!!
						break;
						// !!!
						////////////////////////////////////////////////
					}
				}
			}
			catch (Exception exception)
			{
				service
				.ExceptionLogFile
				?.Logs(this, "获取服务响应并发送至客户端失败，程序异常。", exception);
			}
			finally
			{
				if (webSocket.IsConnectionEnable())
				{
					_idleSocketInfesForSendToClient.Enqueue(socketInfo);
				}
			}

			if (// 所有待发送的任务都已发送完毕。
			_responseInfesNeedSend.IsEmpty()
			// 没有心跳包的发送需求。
			&& IsAnySocketNeedSendHeartbeta == false)
			{
				// !!!
				break;
				// !!!
			}
		}
	}

	#endregion

}
