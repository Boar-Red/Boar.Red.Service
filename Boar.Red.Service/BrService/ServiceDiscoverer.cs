using BaoXia.Utils;
using BaoXia.Utils.ConcurrentTools;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Interfaces;
using Boar.Red.Service.BrService.Constants;
using Boar.Red.Service.BrService.Extensions;
using Boar.Red.Service.BrService.Middlewares;
using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.BrService.ViewModels;
using Boar.Red.Service.BrService.ViewModels.ServiceManager;
using Boar.Red.Service.Utils;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Boar.Red.Service.BrService;

public class ServiceDiscoverer : IDisposable
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ILogFile? BrServiceLogFile;

	public ILogFile? ExceptionLogFile;

	protected readonly bool _isServiceNodeInfoStateMissingToUnknowEnable;

	public bool IsServiceNodeInfoStateMissingToUnknowEnable => _isServiceNodeInfoStateMissingToUnknowEnable;

	public Func<ServiceDiscovererConfig?>? ToGetServiceDiscovererConfig { get; set; }

	public ServiceDiscovererConfig? Config
	{
		get
		{
			if (ToGetServiceDiscovererConfig
			    is Func<ServiceDiscovererConfig> toGetServiceDiscovererConfig)
			{
				return toGetServiceDiscovererConfig();
			}
			return null;
		}
	}

	public IPEndPoint? ListenEndPoint
	{
		get
		{
			if (_udpClient?.Client.LocalEndPoint
			    is IPEndPoint udpClientListenAddress)
			{
				return udpClientListenAddress;
			}
			return null;
		}
	}

	public int ListenPort
	{
		get
		{
			if (ListenEndPoint
			    is IPEndPoint udpClientListenAddress)
			{
				return udpClientListenAddress.Port;
			}
			return 0;
		}
	}

	protected UdpClient? _udpClient = null;
	protected SemaphoreSlim _udpClientLocker = new(1);
	protected int _udpClientListenPortSpecified = 0;

	protected LoopTask? _taskToBroadcastServiceNodeInfo;

	protected bool _isDisable;
	public bool IsDisable => _isDisable;
	protected bool _isDiscoverInfoReceiveEnable;
	public bool IsDiscoverInfoReceiveEnable => _isDiscoverInfoReceiveEnable;
	protected bool _isListenPortAutoIncrease;
	public bool IsListenPortAutoIncrease => _isListenPortAutoIncrease;

	protected ConcurrentQueue<UdpDataPacket> _dataPacketsNeedSend = new();
	protected Tasks _tasksToSendDataPacket;

	protected List<ServiceNodeInfo> _serverManagerNodeInfes = [];
	protected List<ServiceNodeInfo> _serverManagerNodeInfesUpdate = [];
	protected Tasks _taskToUpdateServiceNodeInfesFromServiceManager;

	public BrServiceMiddleware? CurrentBrService { get; set; }

	public Func<ServiceNodeInfo>? ToGetServiceNodeInfo { get; set; }

	public ServiceInfesManager? ServiceFoundInfesManager { get; set; }

	public Action<ServiceDiscoverer, List<ItemFoundInfo<ServiceNodeInfo>>>? ToReceiveServiceNodeInfesFound { get; set; }

	public Action<ServiceDiscoverer, List<ItemFoundInfo<ServiceInfo>>>? ToReceiveServiceInfesFound { get; set; }

	public DateTime StartTime;

	#endregion


	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static ServiceDiscoverer Default()
	{
		return new BrService.ServiceDiscoverer();

	}

	#endregion

	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	/// <summary>
	/// 服务发现器的构造方法。
	/// </summary>
	/// <param name="isDisable">是否禁用服务发现器。</param>
	/// <param name="isReceiveServiceFoundInfes">是否接收服务发现信息。</param>
	/// <param name="isListenPortAutoIncrease">是否自动增长监听接口，常用于客户端。</param>
	/// <param name="isServiceNodeInfoStateMissingToUnknowEnable">是否服务节点失联后，自动设置状态为“未知”，通常服务管理器使用此功能，自动将失联的服务节点信息标记为“未知”，普通服务则不需要。</param>
	/// <param name="toReceiveServiceNodeInfseFound">接收服务节点信息变动的事件。</param>
	/// <param name="toReceiveServiceInfesFound">接收服务信息变动的事件。</param>
	public ServiceDiscoverer(
	    bool isDisable = false,
	    bool isReceiveServiceFoundInfes = true,
	    bool isListenPortAutoIncrease = true,
	    bool isServiceNodeInfoStateMissingToUnknowEnable = false,
	    Action<ServiceDiscoverer, List<ItemFoundInfo<ServiceNodeInfo>>>? toReceiveServiceNodeInfseFound = null,
	    Action<ServiceDiscoverer, List<ItemFoundInfo<ServiceInfo>>>? toReceiveServiceInfesFound = null)
	{
		_isDisable = isDisable;
		_isDiscoverInfoReceiveEnable = isReceiveServiceFoundInfes;
		_isListenPortAutoIncrease = isListenPortAutoIncrease;

		_isServiceNodeInfoStateMissingToUnknowEnable = isServiceNodeInfoStateMissingToUnknowEnable;

		_tasksToSendDataPacket = new(() =>
		{
			var tasksCountToSendDataPacket = Config?.MaxTasksCountToSendDataPacket ?? 0;
			if (tasksCountToSendDataPacket <= 0)
			{
				tasksCountToSendDataPacket = ServiceDiscovererConfig.MaxTasksCountToSendDataPacketDefault;
			}
			return tasksCountToSendDataPacket;
		});

		_taskToUpdateServiceNodeInfesFromServiceManager = new(() =>
		{
			var tasksCountToUpdateServiceNodeInfesFromServiceManager
			= Config?.MaxTasksCountToUpdateServiceNodeInfesFromServiceManager ?? 0;
			if (tasksCountToUpdateServiceNodeInfesFromServiceManager <= 0)
			{
				tasksCountToUpdateServiceNodeInfesFromServiceManager
				= ServiceDiscovererConfig.MaxTasksCountToUpdateServiceNodeInfesFromServiceManagerDefault;
			}
			return tasksCountToUpdateServiceNodeInfesFromServiceManager;
		});

		if (isReceiveServiceFoundInfes)
		{
			ServiceFoundInfesManager = new ServiceInfesManager(DidServiceNodeInfesUpdated, DidServiceInfesUpdated);
		}
		// !!!
		ToReceiveServiceNodeInfesFound = toReceiveServiceNodeInfseFound;
		ToReceiveServiceInfesFound = toReceiveServiceInfesFound;
		// !!!
	}

	~ServiceDiscoverer()
	{
		this.Stop();
		_ = this.ReleaseUdpClientAsync(false);
	}

	protected async Task ReleaseUdpClientAsync(bool isInUdpClientLockSection)
	{
		try
		{
			// !!! 信号量的等待，必须是“try代码块”的第一行。 !!!
			// !!! ⚠ 同一个线程只能获得一次信号量，                     ⚠
			// !!! ⚠ 因此，不能在同一个线程中“Wait”多次，           ⚠
			// !!! ⚠ 后续的“Wait”会因为信号量已经被占用而阻塞。 ⚠
			// !!!
			if (!isInUdpClientLockSection)
			{
				await _udpClientLocker.WaitAsync();
			}

			if (_udpClient == null)
			{
				return;
			}

			_udpClient.Close();
			_udpClient.Dispose();
		}
		finally
		{
			_udpClient = null;
			_udpClientListenPortSpecified = 0;

			if (!isInUdpClientLockSection)
			{
				_udpClientLocker.Release();
			}
		}
	}

	protected async Task<UdpClient?> TryCreateUdpClientAsync(
	    bool isInUdpClientLockSection,
	    bool isNeedRecreateUdpClient = false)
	{
		try
		{
			// !!! 信号量的等待，必须是“try代码块”的第一行。 !!!
			// !!!
			// !!! ⚠ 同一个线程只能获得一次信号量，                     ⚠
			// !!! ⚠ 因此，不能在同一个线程中“Wait”多次，           ⚠
			// !!! ⚠ 后续的“Wait”会因为信号量已经被占用而阻塞。 ⚠
			// !!!
			if (!isInUdpClientLockSection)
			{
				await _udpClientLocker.WaitAsync();
			}

			if (_isDisable)
			{
				return null;
			}

			int udpClientListenPort = Config?.ListenPort ?? 0;
			if (udpClientListenPort <= 0)
			{
				udpClientListenPort = ServiceDiscovererConfig.ListenPortDefault;
			}

			if (isNeedRecreateUdpClient
			    || _udpClient == null
			    || (_isDiscoverInfoReceiveEnable
			    && _udpClientListenPortSpecified != udpClientListenPort))
			{
				// !!! ⚠
				await this.ReleaseUdpClientAsync(true);
				// !!!
				_udpClient = this.DidCreateUdpClient(
				    udpClientListenPort,
				    (exceptionOfReceiveBytes) =>
				    {
					    BrServiceLogFile?.Logs(
		    this,
		    $"服务运行信息，服务发现器，监听用的UdpClient监听时发生异常。",
		    exceptionOfReceiveBytes);

					    ////////////////////////////////////////////////
					    // !!! 
					    TryCreateUdpClientAsync(false, true).Wait();
					    // !!!
					    ////////////////////////////////////////////////
				    });
				_udpClientListenPortSpecified = udpClientListenPort;
				// !!! ⚠

				BrServiceLogFile?.Logs(
				    this,
				    $"服务运行信息，服务发现器，新建了监听用的UdpClient，并开始监听。",
				    null);
			}
			return _udpClient;
		}
		finally
		{
			// !!!
			if (!isInUdpClientLockSection)
			{
				_udpClientLocker.Release();
			}
			// !!!
		}
	}

	public async Task<bool> StartAsync(ILogFile? bxServiceLogFile, ILogFile? exceptionLogFile,
	    Func<ServiceDiscovererConfig?>? toGetServiceDiscovererConfig, bool isInUdpClientLockSection)
	{
		try
		{
			// !!! 信号量的等待，必须是“try代码块”的第一行。 !!!
			// !!!
			// !!! ⚠ 同一个线程只能获得一次信号量，                     ⚠
			// !!! ⚠ 因此，不能在同一个线程中“Wait”多次，           ⚠
			// !!! ⚠ 后续的“Wait”会因为信号量已经被占用而阻塞。 ⚠
			// !!!
			if (!isInUdpClientLockSection)
			{
				await _udpClientLocker.WaitAsync();
			}

			BrServiceLogFile = bxServiceLogFile;
			ExceptionLogFile = exceptionLogFile;
			ToGetServiceDiscovererConfig = toGetServiceDiscovererConfig;

			if (_isDisable)
			{
				return false;
			}

			StartTime = DateTime.Now;

			// !!!
			_udpClient = await TryCreateUdpClientAsync(true);
			// !!!

			_taskToBroadcastServiceNodeInfo ??= new LoopTask(async (cancellationToken) =>
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return false;
				}
				////////////////////////////////////////////////
				// !!!
				await this.DidBroadcastServiceNodeInfoAsync(Config, false);
				// !!!
				////////////////////////////////////////////////
				return true;
			}, () =>
			{
				var broadcastIntervalSeconds = Config?.BroadcastIntervalSeconds ?? 0.0;
				if (broadcastIntervalSeconds <= 0.0)
				{
					broadcastIntervalSeconds = ServiceDiscovererConfig.BroadcastIntervalSecondsDefault;
				}
				return broadcastIntervalSeconds;
			});

			////////////////////////////////////////////////
			// 尝试开启服务发现信息管理器。
			ServiceFoundInfesManager?.Start(
			    exceptionLogFile,
			    () => Config?.IntervalSecondsToUpdateServiceFoundInfes
			    ?? ServiceInfesManager.IntervalSecondsToUpdateServiceFoundInfesDefault,
			    _isServiceNodeInfoStateMissingToUnknowEnable,
			    () => Config?.MissingSecondsToSetServiceNodeInfesStateUnknow
			    ?? ServiceInfesManager.MissingSecondsToSetServiceNodeInfesStateUnknowDefault);
			////////////////////////////////////////////////

		}
		finally
		{
			// !!!
			if (!isInUdpClientLockSection)
			{
				_udpClientLocker.Release();
			}
			// !!!
		}
		return true;
	}

	public void Stop()
	{
		_taskToBroadcastServiceNodeInfo?.Cancel();
	}

	public void SendDataPacketInAsyncQueue(
	    UdpDataPacket? dataPacket,
	    bool isInUdpClientLockSection)
	{
		if (dataPacket == null)
		{
			return;
		}
		_dataPacketsNeedSend.Enqueue(dataPacket);
		_tasksToSendDataPacket.TryRun(async () =>
		{
			while (_dataPacketsNeedSend.TryDequeue(out var dataPacketNeedSend))
			{
				var dataPacketSendObjectIPEndPoint = dataPacketNeedSend.SendObjectIPEndPoint;
				if (dataPacketSendObjectIPEndPoint == null)
				{
					continue;
				}

				var udpClient = await TryCreateUdpClientAsync(false);
				if (udpClient == null)
				{
					continue;
				}

				// !!!
				dataPacketNeedSend.DataPacketSendTime = DateTime.Now;
				// !!!
				var dataPacketNeedSendBytes = UdpDataPacket.CreateBytesWithUdpDataPacket(dataPacketNeedSend, out _);

				try
				{
					// !!! 信号量的等待，必须是“try代码块”的第一行。 !!!
					// !!!
					// !!! ⚠ 同一个线程只能获得一次信号量，                     ⚠
					// !!! ⚠ 因此，不能在同一个线程中“Wait”多次，           ⚠
					// !!! ⚠ 后续的“Wait”会因为信号量已经被占用而阻塞。 ⚠
					// !!!
					if (!isInUdpClientLockSection)
					{
						await _udpClientLocker.WaitAsync();
					}

					//	
					await udpClient.SendAsync(
						dataPacketNeedSendBytes, dataPacketNeedSendBytes.Length, dataPacketSendObjectIPEndPoint);
					//

				}
				catch (Exception exception)
				{
					ExceptionLogFile?.Logs(this, "发送数据包信息失败，程序异常。", exception);
				}
				finally
				{
					// !!!
					if (!isInUdpClientLockSection)
					{
						_udpClientLocker.Release();
					}
					// !!!
				}
			}
		});
	}

	/// <summary>
	/// 增加需要发现的服务信息。
	/// </summary>
	/// <param name="serviceGroupName">需要发现的服务信息所在分组名称。</param>
	/// <param name="serviceName">需要发现的服务名称。</param>
	/// <param name="toDidServiceInfoUpdated">服务信息发生变化时的事件监听。</param>
	/// <returns>返回添加好的服务发现信息。</returns>
	public ServiceNeedFoundInfo? AddServiceNeedFound(
		string? serviceGroupName, string? serviceName, Action<ServiceNeedFoundInfo>? toDidServiceInfoUpdated)
	{
		var serviceNeedFoundInfo = ServiceFoundInfesManager?.AddServiceNeedFound(
			serviceGroupName, serviceName, toDidServiceInfoUpdated);
		{
			TryUpdateServiceNodeInfesFromServiceManager(true);
		}
		return serviceNeedFoundInfo;
	}

#pragma warning disable CA1822 // 将成员标记为 static
	public void SetServiceNodeConnectFailed(ServiceNodeInfo? serviceNodeInfo)
#pragma warning restore CA1822 // 将成员标记为 static
	{
		if (serviceNodeInfo == null)
		{
			return;
		}

		// !!!
		serviceNodeInfo.ServiceState = ServiceState.Offline;
		// !!!
	}

	public async Task<ServiceNodeInfo?> GetServiceNodeInfoToConnectAsync(
	    string? serviceGroupName,
	    string? serviceName,
	    string? serviceVersionMin,
	    string? serviceVersionMax,
	    //
	    CancellationToken cancellationToken)
	{
		var serviceFoundInfesManager = ServiceFoundInfesManager;
		if (serviceFoundInfesManager == null)
		{
			return null;
		}

		var serviceNodeInfo = await serviceFoundInfesManager.GetServiceNodeInfoToConnectAsync(
		    serviceGroupName, serviceName, serviceVersionMin, serviceVersionMax, Config?.ServiceFindTimeoutSeconds ?? 0, cancellationToken);
		if (serviceNodeInfo != null)
		{
			BrServiceLogFile?.Logs(
				this, $"服务运行信息，服务发现器，获取到了“{serviceGroupName}”分组下“{serviceName}”服务的节点信息。", serviceNodeInfo);
			return serviceNodeInfo;
		}
		else
		{
			BrServiceLogFile?.Logs(
				this, $"服务运行信息，服务发现器，无法获取到“{serviceGroupName}”分组下“{serviceName}”服务的节点信息。", null);
		}
		return null;
	}

	public void TryUpdateServiceNodeInfesFromServiceManager(
	    bool isNeedUpdateFromAllServiceManager)
	{
		if (isNeedUpdateFromAllServiceManager
		    && ServiceFoundInfesManager
		    is ServiceInfesManager serviceFoundInfesManager)
		{
			lock (_serverManagerNodeInfes)
			{
				lock (_serverManagerNodeInfesUpdate)
				{
					foreach (var serviceManagerNodeInfo in _serverManagerNodeInfes)
					{
						_serverManagerNodeInfesUpdate.AddOrUpdateServiceNodeInfo(
						    serviceManagerNodeInfo,
						    out _);
					}
				}
			}
		}


		BrServiceLogFile?.Logs(
		    this,
		    $"服务运行信息，服务发现器，服务节点信息刷新，尝试从服务管理器刷服务节点信息， 已添加更新任务。",
		    null);

		_taskToUpdateServiceNodeInfesFromServiceManager.TryRun(() =>
		{
			ServiceNodeInfo[] serverManagerNodeInfesUpdate;
			lock (_serverManagerNodeInfesUpdate)
			{
				if (_serverManagerNodeInfesUpdate.Count < 1)
				{
					return;
				}
				serverManagerNodeInfesUpdate
		    = [.. _serverManagerNodeInfesUpdate];
				_serverManagerNodeInfesUpdate.Clear();
			}


			BrServiceLogFile?.Logs(
		this,
		$"服务运行信息，服务发现器，服务节点信息刷新，尝试从服务管理器刷服务节点信息， 开始从{serverManagerNodeInfesUpdate.Length}个管理器更新服务节点信息。",
		null);


			foreach (var serviceManagerNodeInfo
	    in
	    serverManagerNodeInfesUpdate)
			{
				DidUpdateServiceNodeInfesFromServiceManager(
			serviceManagerNodeInfo);
			}
		});
	}

	#endregion


	////////////////////////////////////////////////
	// @事件节点
	////////////////////////////////////////////////

	#region 事件节点

	protected UdpClient? DidCreateUdpClient(
	    int listenPort,
	    Action<Exception> toProcessReceiveBytesException)
	{
		if (listenPort <= 0)
		{
			return null;
		}

		UdpClient? udpClient = null;
		if (!_isDiscoverInfoReceiveEnable)
		{
			udpClient = new UdpClient()
			{
				EnableBroadcast = true
			};
			return udpClient;
		}
		IPEndPoint? listenIPEndPoint = null;
		while (true)
		{
			try
			{
				listenIPEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
				udpClient = new UdpClient(listenIPEndPoint)
				{
					EnableBroadcast = true
				};
				// !!!
				ServiceConsoleUtil.WriteServiceInfes(
				    "服务发现器端口：" + listenPort + "（最终）");
				break;
				// !!!
			}
			catch (System.Net.Sockets.SocketException exception)
			{
				if (exception.SocketErrorCode == SocketError.AddressAlreadyInUse
				    && _isListenPortAutoIncrease == true)
				{
					listenPort++;
				}
				else
				{
					return null;
				}
			}
		}
		if (listenIPEndPoint == null)
		{
			return null;
		}

		////////////////////////////////////////////////
		udpClient?.StartReceiveBytesUtil(
		    (_) =>
		    {
			    return _isDiscoverInfoReceiveEnable;
		    },
		    listenIPEndPoint,
		    (udpClient, remoteIPEndPoint, bytesReceived, exceptionOfReceiveBytes) =>
		    {
			    if (exceptionOfReceiveBytes != null)
			    {
				    ////////////////////////////////////////////////
				    // !!! 发生异常后，需要重建“_udpClient” !!!
				    toProcessReceiveBytesException.Invoke(exceptionOfReceiveBytes);
				    // !!!
				    ////////////////////////////////////////////////
				    return false;
			    }

			    var udpDataPackageReceiveTime = DateTime.Now;
			    if (bytesReceived == null
		|| bytesReceived.Length < 1)
			    {
				    return true;
			    }
			    if (!_isDiscoverInfoReceiveEnable)
			    {
				    return true;
			    }

			    try
			    {
				    var bytesOperator = new BytesOperator(bytesReceived);
				    var dataPacketName = bytesOperator.ReadString();
				    if (string.IsNullOrEmpty(dataPacketName))
				    {
					    return true;
				    }

				    if (!dataPacketName.Equals(UdpDataPackageNames.BrService_UdpDataPackage_ServiceNodeInfo))
				    {
					    return true;
				    }

				    var serviceNodeInfo
			= UdpDataPacket.CreateUdpDataPacketWithBytes<ServiceNodeInfo>(
			ref bytesOperator,
			true);
				    if (serviceNodeInfo == null)
				    {
					    return true;
				    }

				    serviceNodeInfo.DataPacketReceiveTime = udpDataPackageReceiveTime;
				    serviceNodeInfo.UdpEndPointIPAddress = remoteIPEndPoint?.Address;
				    serviceNodeInfo.ServiceDiscovererEndPoint = remoteIPEndPoint;

				    // !!!
				    this.DidReceiveServiceNodeInfo(serviceNodeInfo);
				    // !!!
			    }
			    catch (Exception exception)
			    {
				    ExceptionLogFile?.Logs(this, "解析数据包信息失败，程序异常。", exception);
			    }
			    return true;
		    });
		////////////////////////////////////////////////


		return udpClient;
	}

	protected async Task DidBroadcastServiceNodeInfoAsync(ServiceDiscovererConfig? serviceDiscoveryConfig, bool isInUdpClientLockSection)
	{
		if (serviceDiscoveryConfig == null)
		{
			return;
		}
		if (_isDisable)
		{
			return;
		}

		ServiceNodeInfo? serviceNodeInfo = null;
		if (ToGetServiceNodeInfo != null)
		{
			serviceNodeInfo = ToGetServiceNodeInfo();
		}
		if (serviceNodeInfo == null
		    && CurrentBrService != null)
		{
			serviceNodeInfo = CurrentBrService.ServiceNodeInfo;
		}
		if (serviceNodeInfo == null)
		{
			return;
		}

		try
		{
			// !!! 信号量的等待，必须是“try代码块”的第一行。 !!!
			// !!!
			// !!! ⚠ 同一个线程只能获得一次信号量，                     ⚠
			// !!! ⚠ 因此，不能在同一个线程中“Wait”多次，           ⚠
			// !!! ⚠ 后续的“Wait”会因为信号量已经被占用而阻塞。 ⚠
			// !!!
			if (!isInUdpClientLockSection)
			{
				await _udpClientLocker.WaitAsync();
			}

			// !!!
			UdpClient? udpClient = await TryCreateUdpClientAsync(true);
			// !!!
			if (udpClient == null)
			{
				return;
			}

			////////////////////////////////////////////////
			// !!!
			serviceNodeInfo.DataPacketSendTime = DateTime.Now;
			// !!!
			var serviceNodeInfoUdpDataPackageBytes
			    = UdpDataPacket.CreateBytesWithUdpDataPacket(serviceNodeInfo, out _);
			// 如果指定了服务管理器地址，则不再进行广播，而是直连。
			if (!IPAddress.TryParse(
			    serviceDiscoveryConfig.ServiceManagerAddress,
			    out IPAddress? serviceManagerAddress))
			{
				serviceManagerAddress = IPAddress.Broadcast;
			}

			////////////////////////////////////////////////
			await udpClient.SendAsync(
			    serviceNodeInfoUdpDataPackageBytes,
			    serviceNodeInfoUdpDataPackageBytes.Length,
			    new IPEndPoint(
			    serviceManagerAddress,
			    serviceDiscoveryConfig.ServiceManagerDiscoveryListenPort));
			////////////////////////////////////////////////
		}
		catch (Exception exception)
		{
			ExceptionLogFile?.Logs(this, "广播服务节点信息失败，程序异常。", exception);
		}
		finally
		{
			// !!!
			if (!isInUdpClientLockSection)
			{
				_udpClientLocker.Release();
			}
			// !!!
		}
	}

	public void DidReceiveServiceNodeInfo(
	    ServiceNodeInfo serviceNodeInfo)
	{
		if (_isDisable)
		{
			return;
		}

		// Console.WriteLine($"{DateTime.Now:HH:mm ss}，@test，服务发现器，收到了服务节点信息，{serviceNodeInfo.ServiceName}，命令 {serviceNodeInfo.SyncCommand}。");

		// !!! 尝试将服务节点信息，加入服务信息管理器                            !!!
		// !!!⚠ 后续的事件链，由“ServiceFoundInfesManager”触发 ⚠!!!
		ServiceFoundInfesManager?.AddOrUpdateServiceNodeInfo(
		    serviceNodeInfo,
		    out _);
	}

	protected void DidServiceNodeInfesUpdated(List<ItemFoundInfo<ServiceNodeInfo>> serviceNodeInfoFoundInfes)
	{
		BrServiceLogFile?.Logs(
		    this,
		    "服务运行信息，服务发现器，发现新的服务节点信息。",
		    serviceNodeInfoFoundInfes.ToJsonString());

		////////////////////////////////////////////////
		// 通知服务更新。
		////////////////////////////////////////////////
		ToReceiveServiceNodeInfesFound?.Invoke(
		    this,
		    serviceNodeInfoFoundInfes);

		////////////////////////////////////////////////
		// 特殊服务信息的特殊处理。
		////////////////////////////////////////////////
		foreach (var serviceNodeFoundInfo in serviceNodeInfoFoundInfes)
		{
			var serviceNodeInfo = serviceNodeFoundInfo.Item;

			if (BuiltInServiceNames.ServiceManager.EqualsIgnoreCase(
			    serviceNodeInfo.ServiceName))
			{
				//
				// Console.WriteLine($"{DateTime.Now:HH:mm ss}，@test，服务发现器，收到了服务管理节点信息已更新的消息，发现类型为{serviceNodeFoundInfo.FoundType.Name()}。");
				//
				DidServiceManagerNodeInfoUpdated(serviceNodeInfo);
			}
		}
	}

	protected void DidServiceManagerNodeInfoUpdated(
	    ServiceNodeInfo serviceManagerNodeInfo)
	{
		var serviceManagerHttpAddress
		    = serviceManagerNodeInfo.FinalHttpUri;
		if (string.IsNullOrEmpty(serviceManagerHttpAddress))
		{
			return;
		}

		var serviceManagerNodeInfoFoundType = ItemFoundType.NotFound;
		lock (_serverManagerNodeInfesUpdate)
		{
			_serverManagerNodeInfes.AddOrUpdateServiceNodeInfo(
			    serviceManagerNodeInfo,
			    out serviceManagerNodeInfoFoundType);
			_serverManagerNodeInfesUpdate
			    .AddOrUpdateServiceNodeInfo(
			    serviceManagerNodeInfo,
			    out _);
		}

		//
		// Console.WriteLine($"{DateTime.Now:HH:mm ss}，@test，服务发现器，处理服务管理器节点更新，开始。");
		//
		if (serviceManagerNodeInfoFoundType.IsNotOrNormalFound()
		    && serviceManagerNodeInfo.SyncCommand == ServiceNodeInfoSyncCommand.None)
		{
			//
			// Console.WriteLine($"{DateTime.Now:HH:mm ss}，@test，服务发现器，处理服务管理器节点更新，结束，命令为空。");
			//
			return;
		}

		// !!! 服务管理器，不响应自身的服务节点信息。 !!!
		ServiceNodeInfo? currentServiceNodeInfo = null;
		if (ToGetServiceNodeInfo != null)
		{
			currentServiceNodeInfo = ToGetServiceNodeInfo();
		}
		if (currentServiceNodeInfo == null
		    && CurrentBrService != null)
		{
			currentServiceNodeInfo = CurrentBrService.ServiceNodeInfo;
		}

		if (serviceManagerNodeInfo.IsSameServiceNode(
		    currentServiceNodeInfo))
		{
			//
			// Console.WriteLine($"{DateTime.Now:HH:mm ss}，@test，服务发现器，处理服务管理器节点更新，结束，自身更新。");
			//
			return;
		}

		if (serviceManagerNodeInfo.SyncCommand != ServiceNodeInfoSyncCommand.None)
		{
			BrServiceLogFile?.Logs(
			    this,
			    $"服务运行信息，服务发现器，收到了服务管理器节点信息命令：{serviceManagerNodeInfo.SyncCommand}。",
			    serviceManagerNodeInfo);
		}
		else if (serviceManagerNodeInfoFoundType == ItemFoundType.NewItemFound)
		{
			BrServiceLogFile?.Logs(
			    this,
			    "服务运行信息，服务发现器，发现了已有的服务管理器节点信息更新。",
			    serviceManagerNodeInfo);
		}
		else
		{
			BrServiceLogFile?.Logs(
			    this,
			    "服务运行信息，服务发现器，发现了新的服务管理器节点信息。",
			    serviceManagerNodeInfo);
		}

		//
		// Console.WriteLine($"{DateTime.Now:HH:mm ss}，@test，服务发现器，处理服务管理器节点更新，结束，尝试更新服务节点信息。");
		//

		// !!!
		TryUpdateServiceNodeInfesFromServiceManager(false);
		// !!!
	}

	protected async void DidUpdateServiceNodeInfesFromServiceManager(
	    ServiceNodeInfo serviceManagerNodeInfo)
	{
		try
		{
			if (ServiceFoundInfesManager
			    is not ServiceInfesManager serviceFoundInfesManager)
			{
				return;
			}

			var serverManagerApiUriRoot
			    = serviceManagerNodeInfo.FinalHttpUri
			    ?.ToUriSystemDirectoryPath();
			if (string.IsNullOrEmpty(serverManagerApiUriRoot))
			{
				return;
			}

			var httpClient = BrServiceHttpClient.HttpClient;
			var gerServiceNodeInfesApiUrl
			    = serverManagerApiUriRoot
			    + BuiltInServiceApiUri.ServiceManager.ServiceRegisterInfo.GetServiceRegisterInfes
			    .ToUriSystemRelativePath();

			var request = new GetServiceRegisterInfesRequest();
			var response = await httpClient.PostToJsonApiAsync<GetServiceRegisterInfesResponse>(
			    gerServiceNodeInfesApiUrl,
			    request,
			    null);
			if (response?.ServiceRegisterInfes
			    is not List<ServiceInfo> serviceRegisterInfes)
			{
				return;
			}
			if (serviceRegisterInfes.Count < 1)
			{
				return;
			}


			//
			// Console.WriteLine($"{DateTime.Now:HH:mm ss}，@test，服务发现器，从服务管理器更新了服务注册信息，共收到 {serviceRegisterInfes.Count} 个服务节点注册信息。");
			foreach (var serviceRegisterInfo in serviceRegisterInfes)
			{
				foreach (var serviceNodeInfo in serviceRegisterInfo.NodeInfes)
				{
					//
					// Console.WriteLine($"{DateTime.Now:HH:mm ss}，@test，服务发现器，{serviceNodeInfo.ServiceName}，{serviceNodeInfo.ServiceStateName}。");
					//
				}
			}
			//


			var allServiceNodeInfesCount = 0;
			var validServiceNodeInfesCount = 0;
			foreach (var serviceRegisterInfo in serviceRegisterInfes)
			{
				var serviceNodeInfes = serviceRegisterInfo.NodeInfes;
				if (ArrayUtil.IsEmpty(serviceNodeInfes))
				{
					continue;
				}

				var serviceInfesUpdated = new List<ServiceBaseInfo>();
				foreach (var serviceNodeInfo in serviceNodeInfes)
				{
					// !!! “AddOrUpdateServiceNodeInfo”会触发 !!!
					// !!! “serviceFoundInfesManager。DidServiceNodeInfesUpdated”事件。 !!!
					// !!!
					serviceNodeInfo.DataPacketReceiveTime = DateTime.Now;
					// !!!
					serviceFoundInfesManager.AddOrUpdateServiceNodeInfo(
					    serviceNodeInfo,
					    out var _);
					// !!!
					allServiceNodeInfesCount++;
					if (serviceNodeInfo.ServiceState != ServiceState.Offline)
					{
						validServiceNodeInfesCount++;
					}
				}
			}

			BrServiceLogFile?.Logs(
			    this,
			    $"服务运行信息，服务发现器，服务节点信息刷新，从服务管理器获取到了 {allServiceNodeInfesCount} 个新的服务节点信息，其中 {validServiceNodeInfesCount} 个为可用节点信息。",
			    serviceManagerNodeInfo);
		}
		catch (Exception exception)
		{
			//
			lock (_serverManagerNodeInfes)
			{
				for (var serverManagerNodeInfoIndex = _serverManagerNodeInfes.Count - 1;
				    serverManagerNodeInfoIndex >= 0;
				    serverManagerNodeInfoIndex--)
				{
					var serverManagerNodeInfoExisted
					    = _serverManagerNodeInfes[serverManagerNodeInfoIndex];
					if (serverManagerNodeInfoExisted.IsSameServiceNode(serviceManagerNodeInfo))
					{
						_serverManagerNodeInfes.RemoveAt(serverManagerNodeInfoIndex);
						//
						BrServiceLogFile?.Logs(
						    this,
						    "服务运行信息，服务发现器，从服务节点更新服务信息失败，程序异常，移除服务管理器节点信息。",
						    serviceManagerNodeInfo);
						//
						break;
					}
				}
			}
			//
			ExceptionLogFile?.Logs(
			    this,
			    "从服务节点更新服务信息失败，程序异常。",
			    new object[]
			    {
	    serviceManagerNodeInfo,
	    exception
			    });
		}
	}

	protected void DidServiceInfesUpdated(List<ItemFoundInfo<ServiceInfo>> serviceInfoFoundInfes)
	{
		ToReceiveServiceInfesFound?.Invoke(
		    this,
		    serviceInfoFoundInfes);
	}

	#endregion


	////////////////////////////////////////////////
	// @实现“IDisposable”
	////////////////////////////////////////////////

	#region 实现“IDisposable”

	public async void Dispose()
	{
		Stop();
		await ReleaseUdpClientAsync(false);

		////////////////////////////////////////////////
		GC.SuppressFinalize(this);
		////////////////////////////////////////////////
	}

	#endregion
}