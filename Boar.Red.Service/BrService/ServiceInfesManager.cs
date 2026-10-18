using BaoXia.Utils;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Interfaces;
using Boar.Red.Service.BrService.Constants;
using Boar.Red.Service.BrService.Extensions;
using Boar.Red.Service.BrService.ViewModels;

namespace Boar.Red.Service.BrService;

public class ServiceInfesManager(
    Action<List<ItemFoundInfo<ServiceNodeInfo>>>? toDidServiceNodeInfesUpdated,
    Action<List<ItemFoundInfo<ServiceInfo>>>? toDidServiceInfesUpdated)
{
	////////////////////////////////////////////////
	// @静态常量
	////////////////////////////////////////////////

	#region 静态常量

	public const double IntervalSecondsToUpdateServiceFoundInfesDefault = 1.0;

	//public const bool IsMissingSecondsToSetServiceNodeInfesStateUnknowEnableDefault = false;

	public const double MissingSecondsToSetServiceNodeInfesStateUnknowDefault = 5.0;

	#endregion


	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	private ILogFile? _exceptionLogFile = null;

	private ServiceNeedFoundInfo[] _serviceInfesNeedFound = [];


	public Func<double>? ToGetIntervalSecondsToUpdateServiceFoundInfes { get; set; } = null;

	private bool _isServiceNodeInfoStateMissingToUnknowEnable = false;
	public bool IsServiceNodeInfoStateMissingToUnknowEnable => _isServiceNodeInfoStateMissingToUnknowEnable;
	public Func<double>? ToGetMissingSecondsToSetServiceNodeInfesStateUnknow { get; set; } = null;


	private int _serviceFoundInfoGenerateIdSeek = 0;

	private int _serviceNodeInfoGenerateIdSeek = 0;

	private readonly object _serviceFoundInfesLocker = new();
	private ServiceInfo[] _serviceFoundInfes = [];

	private readonly List<ServiceNodeInfo> _newServiceNodeInfes = [];

	private LoopTask? _taskToUpdateServiceNodeInfes = null;

	public Action<List<ItemFoundInfo<ServiceNodeInfo>>>? ToDidServiceNodeInfesUpdated { get; set; } = toDidServiceNodeInfesUpdated;
	public Action<List<ItemFoundInfo<ServiceInfo>>>? ToDidServiceInfesUpdated { get; set; } = toDidServiceInfesUpdated;

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public bool TryProcessServiceNodeInfesNeedUpdated()
	{
		try
		{
			if (!Monitor.TryEnter(_serviceFoundInfesLocker))
			{
				return false;
			}

			////////////////////////////////////////////////
			// 1/5，检测当前是否需要更新服务节点信息：
			////////////////////////////////////////////////

			while (true)
			{
				List<ServiceNodeInfo>? newServiceNodeInfes = null;
				lock (_newServiceNodeInfes)
				{
					if (_newServiceNodeInfes.Count > 0)
					{
						newServiceNodeInfes = [.. _newServiceNodeInfes];
						_newServiceNodeInfes.Clear();
					}
				}
				if (newServiceNodeInfes == null
				|| newServiceNodeInfes.Count < 1)
				{
					return true;
				}

				////////////////////////////////////////////////
				// 2/5，重新生成服务节点信息：
				////////////////////////////////////////////////
				var now = DateTime.Now;
				var serviceInfesFound = new List<ServiceInfo>(_serviceFoundInfes);
				var serviceNodeInfesWithServiceInfoChanged = new List<ItemFoundInfo<ServiceNodeInfo>>();
				while (newServiceNodeInfes.Count > 0)
				{
					var currentNewServiceNodeInfoIndex = newServiceNodeInfes.Count - 1;
					var currentNewServiceNodeInfo = newServiceNodeInfes[currentNewServiceNodeInfoIndex];
					var currentServiceFoundInfo = serviceInfesFound.TryAddServiceInfo(
					    currentNewServiceNodeInfo.GroupName,
					    currentNewServiceNodeInfo.ServiceName);

					var currentServiceNodeInfes
					    = new List<ServiceNodeInfo>(currentServiceFoundInfo.NodeInfes);
					{
						// 
						newServiceNodeInfes.RemoveAt(currentNewServiceNodeInfoIndex);
						//
						currentNewServiceNodeInfo
						    = currentServiceNodeInfes.AddOrUpdateServiceNodeInfo(
						    currentNewServiceNodeInfo,
						    out var serviceNodeFoundType);
						if (serviceNodeFoundType.IsNewOrUpdatedFound()
						    // 如：服务管理器，可能存在服务信息为变化，
						    // 但是需要通过命令进行状态信息更新的需要。
						    || currentNewServiceNodeInfo.SyncCommand != ServiceNodeInfoSyncCommand.None)
						{
							// !!!
							serviceNodeInfesWithServiceInfoChanged.Add(
							    new(currentNewServiceNodeInfo,
							    serviceNodeFoundType));
							// !!!

							if (currentNewServiceNodeInfo.Id == 0)
							{
								// !!!⚠
								currentNewServiceNodeInfo.Id = GenerateServiceNodeId();
								currentNewServiceNodeInfo.CreateTime = now;
								// !!!⚠
							}
							currentNewServiceNodeInfo.ServiceInfoChangeTime = now;
							currentServiceFoundInfo.ServiceInfoChangeTime = now;
						}
						currentNewServiceNodeInfo.UpdateTime = now;
						currentServiceFoundInfo.UpdateTime = now;
					}

					////////////////////////////////////////////////

					for (var prevNewServiceNodeInfoIndex = currentNewServiceNodeInfoIndex - 1;
					prevNewServiceNodeInfoIndex >= 0;
					prevNewServiceNodeInfoIndex--)
					{
						var prevNewServiceNodeInfo = newServiceNodeInfes[prevNewServiceNodeInfoIndex];
						if (!prevNewServiceNodeInfo.IsSameService(currentNewServiceNodeInfo))
						{
							continue;
						}
						// !!!
						newServiceNodeInfes.RemoveAt(prevNewServiceNodeInfoIndex);
						// !!!
						prevNewServiceNodeInfo
						    = currentServiceNodeInfes.AddOrUpdateServiceNodeInfo(
						    prevNewServiceNodeInfo,
						    out var serviceNodeFoundType);
						if (serviceNodeFoundType.IsNewOrUpdatedFound())
						{
							// !!!
							serviceNodeInfesWithServiceInfoChanged.Add(
							    new(prevNewServiceNodeInfo,
							    serviceNodeFoundType));
							// !!!

							if (prevNewServiceNodeInfo.Id == 0)
							{
								// !!!⚠
								prevNewServiceNodeInfo.Id = GenerateServiceNodeId();
								prevNewServiceNodeInfo.CreateTime = now;
								// !!!⚠
							}
							prevNewServiceNodeInfo.ServiceInfoChangeTime = now;
							currentServiceFoundInfo.ServiceInfoChangeTime = now;
						}
						prevNewServiceNodeInfo.UpdateTime = now;
						currentServiceFoundInfo.UpdateTime = now;
					}

					currentServiceNodeInfes.Sort((serviceNodeInfoA, serviceNodeInfoB) =>
					{
						var compareResult
		    = string.Compare(
		    serviceNodeInfoA.NodeName,
		    serviceNodeInfoB.NodeName);
						{ }
						return compareResult;
					});
					// !!!
					currentServiceFoundInfo.NodeInfes = [.. currentServiceNodeInfes];
					// !!!
				}

				////////////////////////////////////////////////
				// 3/5，更新服务，服务节点信息：
				////////////////////////////////////////////////

				var serviceInfeWithServiceInfoChanged = new List<ItemFoundInfo<ServiceInfo>>();
				double serviceNodeInfesMissingSecondsToInvalid
				    = MissingSecondsToSetServiceNodeInfesStateUnknowDefault;
				if (ToGetMissingSecondsToSetServiceNodeInfesStateUnknow != null)
				{
					serviceNodeInfesMissingSecondsToInvalid
					= ToGetMissingSecondsToSetServiceNodeInfesStateUnknow();
				}
				var timeToMissServiceNodeInfes
				= now.AddSeconds(-serviceNodeInfesMissingSecondsToInvalid);
				foreach (var serviceFoundInfo in serviceInfesFound)
				{
					var serviceLoadRate = 0.0;
					var serviceNodeInfes = serviceFoundInfo.NodeInfes;
					var serviceNodeInfesCount_UnknowState = 0;
					var serviceNodeInfesCount_InBooting = 0;
					var serviceNodeInfesCount_InService = 0;
					var serviceNodeInfesCount_InShutdowning = 0;
					var serviceNodeInfesCount_InShutdown = 0;
					foreach (var serviceNodeInfo in serviceNodeInfes)
					{
						if (_isServiceNodeInfoStateMissingToUnknowEnable
						    && serviceNodeInfo.DataPacketReceiveTime <= timeToMissServiceNodeInfes)
						{
							if (serviceNodeInfo.ServiceState != ServiceState.Offline)
							{
								//
								// Console.WriteLine(
								//	$"{DateTime.Now:HH:mm ss}，@test，服务信息管理器，通过上一次的数据接收时间，设置 {serviceNodeInfo.ServiceName} 节点为“无效状态”。");
								//

								// !!!
								serviceNodeInfo.ServiceState = ServiceState.Offline;
								// !!!
								var isServiceNodeInfoInServiceNodeInfesWithServiceInfoChanged = false;
								foreach (var serviceNodeInfoWithServiceInfoChanged
								    in serviceNodeInfesWithServiceInfoChanged)
								{
									if (serviceNodeInfo == serviceNodeInfoWithServiceInfoChanged.Item)
									{
										isServiceNodeInfoInServiceNodeInfesWithServiceInfoChanged = true;
										break;
									}
								}
								if (isServiceNodeInfoInServiceNodeInfesWithServiceInfoChanged == false)
								{
									serviceNodeInfesWithServiceInfoChanged.Add(
									    new(serviceNodeInfo,
									    ItemFoundType.UpdatedItemFound));
								}
								// !!!
							}
						}

						switch (serviceNodeInfo.ServiceState)
						{
							case ServiceState.InService:
								{
									serviceNodeInfesCount_InService++;
									serviceLoadRate += serviceNodeInfo.LoadRate;
								}
								break;
							default:
							case ServiceState.Offline:
								{
									serviceNodeInfesCount_UnknowState++;
									serviceLoadRate += 1.0;
								}
								break;
							case ServiceState.InBooting:
								{
									serviceNodeInfesCount_InBooting++;
									serviceLoadRate += 1.0;
								}
								break;
							case ServiceState.InShuttingDown:
								{
									serviceNodeInfesCount_InShutdowning++;
									serviceLoadRate += 1.0;
								}
								break;
							case ServiceState.InShutdown:
								{
									serviceNodeInfesCount_InShutdown++;
									serviceLoadRate += 1.0;
								}
								break;
						}
					}
					// !!!
					serviceFoundInfo.LoadRate
					    = serviceNodeInfes.Length > 0
					    ? (serviceLoadRate / serviceNodeInfes.Length)
					    : 0;
					// !!!

					var serviceState = ServiceState.Offline;
					var serviceNodeInfesCount = serviceNodeInfes.Length;
					if (serviceNodeInfesCount_InService > 0)
					{
						serviceState = ServiceState.InService;
					}
					else if (serviceNodeInfesCount_UnknowState == serviceNodeInfesCount)
					{
						serviceState = ServiceState.Offline;
					}
					else if (serviceNodeInfesCount_InBooting > 0)
					{
						serviceState = ServiceState.InBooting;
					}
					else if (serviceNodeInfesCount_InShutdowning == serviceNodeInfesCount)
					{
						serviceState = ServiceState.InShuttingDown;
					}
					else if (serviceNodeInfesCount_InShutdown == serviceNodeInfesCount)
					{
						serviceState = ServiceState.InShutdown;
					}
					else
					{
						serviceState = ServiceState.Offline;
					}

					var serviceFoundInfoFoundType = ItemFoundType.NormalFound;
					if (serviceFoundInfo.ServiceState != serviceState)
					{
						// !!!
						serviceFoundInfo.ServiceState = serviceState;
						serviceFoundInfo.ServiceInfoChangeTime = now;
						serviceFoundInfoFoundType = ItemFoundType.UpdatedItemFound;
						// !!!
					}
					if (serviceFoundInfoFoundType.IsNewOrUpdatedFound())
					{
						if (serviceFoundInfo.Id == 0)
						{
							// !!!⚠ 设置新的服务注册信息Id ⚠
							serviceFoundInfo.Id = GenerateServiceFoundInfoId();
							serviceFoundInfo.CreateTime = now;
							serviceFoundInfoFoundType = ItemFoundType.NewItemFound;
							// !!!⚠
						}
						//
						serviceInfeWithServiceInfoChanged.Add(
						    new(serviceFoundInfo,
						    serviceFoundInfoFoundType));
						//
						serviceFoundInfo.UpdateTime = now;
						serviceFoundInfo.ServiceInfoChangeTime = now;
					}
				}

				serviceInfesFound.Sort((serviceFoundInfoA, serviceFoundInfoB) =>
				{
					var compareResult
		= string.Compare(
		    serviceFoundInfoB.GroupName,
		    serviceFoundInfoB.GroupName);
					if (compareResult != 0)
					{
						return compareResult;
					}

					compareResult
		= string.Compare(
		    serviceFoundInfoB.ServiceName,
		    serviceFoundInfoB.ServiceName);
					{ }
					return compareResult;
				});


				////////////////////////////////////////////////
				// 4/5，更新服务信息：
				////////////////////////////////////////////////
				// !!!⚠ 更新服务信息 ⚠!!!
				_serviceFoundInfes = [.. serviceInfesFound];
				// !!!

				////////////////////////////////////////////////
				// 5/5，通知服务信息更新：
				////////////////////////////////////////////////
				if (serviceNodeInfesWithServiceInfoChanged.Count > 0)
				{
					foreach (var serviceNodeInfoWithServiceInfoChanged in serviceNodeInfesWithServiceInfoChanged)
					{
						var serviceNodeInfo = serviceNodeInfoWithServiceInfoChanged.Item;
						//
						// Console.WriteLine($"{DateTime.Now:HH:mm ss}，@test，服务信息管理器，{serviceNodeInfo.ServiceName}，{serviceNodeInfo.ServiceStateName}。");
						//
					}
					//
					// Console.WriteLine(
					// $"{DateTime.Now:HH:mm ss}，@test，服务信息管理器，发现了 {serviceNodeInfesWithServiceInfoChanged.Count}个服务节点信息变更。");
					//

					// !!!
					ToDidServiceNodeInfesUpdated?.Invoke(serviceNodeInfesWithServiceInfoChanged);
					// !!!
				}
				if (serviceInfeWithServiceInfoChanged.Count > 0)
				{
					foreach (var serviceFoundInfo in serviceInfeWithServiceInfoChanged)
					{
						var serviceInfo = serviceFoundInfo.Item;
						var serviceInfoNeedFound
						    = _serviceInfesNeedFound.GetServiceInfoWithGroupName(
						    serviceInfo.GroupName,
						    serviceInfo.ServiceName);
						// !!!
						serviceInfoNeedFound?.SetServiceInfoUpdated();
						// !!!
					}

					// !!!
					ToDidServiceInfesUpdated?.Invoke(serviceInfeWithServiceInfoChanged);
					// !!!
				}
			}
		}
		catch (Exception exception)
		{
			_exceptionLogFile?.Logs(this, "处理待更新的服务节点信息失败，程序异常。", exception);
		}
		finally
		{
			// !!!
			Monitor.Exit(_serviceFoundInfesLocker);
			// !!!
		}
		return true;
	}

	protected int GenerateServiceFoundInfoId()
	{
		return Interlocked.Increment(ref _serviceFoundInfoGenerateIdSeek);
	}

	protected int GenerateServiceNodeId()
	{
		return Interlocked.Increment(ref _serviceNodeInfoGenerateIdSeek);
	}

	public ServiceNodeInfo? AddOrUpdateServiceNodeInfo(
	    ServiceNodeInfo? serviceNodeInfo,
	    out ItemFoundType serviceNodeInfoFoundType)
	{
		serviceNodeInfoFoundType = ItemFoundType.NotFound;

		if (serviceNodeInfo == null)
		{
			return null;
		}

		//
		// 所有服务都认为有效，方便新接入的客户端可以立即获得到最新的服务节点信息。
		//
		//var isValidServiceNode = false;
		// 开启了接受任意服务节点的功能。
		//if (_isAnyServiceInfoNeedFound)
		//{
		//	isValidServiceNode = true;
		//}
		// 服务管理器节点作为特殊节点，总是有效。
		//else if (BuiltInServiceNames.ServiceManager
		//	.EqualsIgnoreCase(serviceNodeInfo.ServiceName))
		//{
		//	isValidServiceNode = true;
		//}
		//else
		//{
		//	// 只有注册的服务信息，才会有效。
		//	var serviceInfoNeedFound
		//	= _serviceInfesNeedFound.GetServiceInfoWithGroupName(
		//	serviceNodeInfo.GroupName,
		//	serviceNodeInfo.ServiceName);
		//	if (serviceInfoNeedFound != null)
		//	{
		//		// 当前需要的服务信息。
		//		isValidServiceNode = true;
		//	}
		//}
		//if (!isValidServiceNode)
		//{
		//	return null;
		//}

		lock (_newServiceNodeInfes)
		{
			return _newServiceNodeInfes
			    .AddOrUpdateServiceNodeInfo(
			    serviceNodeInfo,
			    out _);
		}
	}

	public void RemoveServiceNodeInfo(ServiceNodeInfo? serviceNodeInfo)
	{
		if (serviceNodeInfo == null)
		{
			return;
		}

		var serviceInfesFound
		    = GetServiceFoundInfesWithGroupName(
		    serviceNodeInfo.GroupName,
		    serviceNodeInfo.ServiceName);
		if (ListUtil.IsEmpty(serviceInfesFound))
		{
			return;
		}

		foreach (var serviceFoundInfo in serviceInfesFound)
		{
			// !!!
			serviceFoundInfo.RemoveServiceNodeInfo(serviceNodeInfo);
			// !!!
		}
	}

	public void EnumerateAllServiceNodeInfo(Func<ServiceNodeInfo, bool> toReceiveServiceNodeInfo)
	{
		var allServiceFoundInfes = _serviceFoundInfes;
		foreach (var serviceFoundInfo in allServiceFoundInfes)
		{
			var serviceNodeInfes = serviceFoundInfo.NodeInfes;
			if (serviceNodeInfes.Length < 1)
			{
				continue;
			}

			var isBreakToNextServiceFoundInfo = false;
			foreach (var serviceNodeInfo in serviceNodeInfes)
			{
				if (!toReceiveServiceNodeInfo(serviceNodeInfo))
				{
					isBreakToNextServiceFoundInfo = true;
					break;
				}
			}
			if (isBreakToNextServiceFoundInfo)
			{
				break;
			}
		}
	}

	public List<ServiceInfo> GetServiceFoundInfesWithGroupName(
	    string? groupName,
	    string? serviceName,
	    string? nodeName = null,
	    string? serviceVersionMin = null,
	    string? serviceVersionMax = null)
	{
		var serviceInfesFound = new List<ServiceInfo>();
		var allServiceFoundInfes = _serviceFoundInfes;
		var isGroupNameSpecified = groupName?.Length > 0;
		var isServiceNameSpecified = serviceName?.Length > 0;
		var isNodeNameSpecified = nodeName?.Length > 0;
		var serviceVersionMinObject
		    = serviceVersionMin != null
		    ? new Version(serviceVersionMin)
		    : null;
		var serviceVersionMaxObject
		    = serviceVersionMax != null
		    ? new Version(serviceVersionMax)
		    : null;
		foreach (var serviceFoundInfo
		    in
		    allServiceFoundInfes)
		{
			if (isGroupNameSpecified
			    && StringExtension.IndexOfIgnoreCase(
			    serviceFoundInfo.GroupName,
			    groupName) < 0)
			{
				continue;
			}
			if (isServiceNameSpecified
			    && StringExtension.IndexOfIgnoreCase(
			    serviceFoundInfo.ServiceName,
			    serviceName) < 0)
			{
				continue;
			}

			if (isNodeNameSpecified
			    || serviceVersionMinObject != null
			    || serviceVersionMaxObject != null)
			{
				var serviceNodeInfes = new List<ServiceNodeInfo>(0);
				var allNodeInfes = serviceFoundInfo.NodeInfes;
				foreach (var nodeInfo in allNodeInfes)
				{
					if (isNodeNameSpecified
					    && StringExtension.IndexOfIgnoreCase(
					    nodeInfo.NodeName,
					    nodeName) < 0)
					{
						continue;
					}
					if (serviceVersionMinObject != null
					    && serviceVersionMinObject > nodeInfo.ServiceVersionObject)
					{
						continue;
					}
					if (serviceVersionMaxObject != null
					    && serviceVersionMaxObject < nodeInfo.ServiceVersionObject)
					{
						continue;
					}

					// !!!
					serviceNodeInfes.Add(nodeInfo);
					// !!!
				}

				var serviceFoundInfoCloned = new ServiceInfo(
				    serviceFoundInfo.Id,
				    serviceFoundInfo.GroupName,
				    serviceFoundInfo.ServiceName);
				{
					serviceFoundInfoCloned.NodeInfes = [.. serviceNodeInfes];
				}
				serviceInfesFound.Add(serviceFoundInfoCloned);
			}
			else
			{
				serviceInfesFound.Add(serviceFoundInfo);
			}
		}
		return serviceInfesFound;
	}


	public List<ServiceNodeInfo> GetServiceNodeFoundInfesWithGroupName(
	    string? groupName,
	    string? serviceName,
	    string? serviceVersionMin = null,
	    string? serviceVersionMax = null,
	    ServiceState? serviceState = null)
	{
		var serviceNodeFoundInfes = new List<ServiceNodeInfo>();
		var allServiceFoundInfes = _serviceFoundInfes;
		var isGroupNameSpecified = groupName?.Length > 0;
		var isServiceNameSpecified = serviceName?.Length > 0;
		var serviceVersionMinObject
		    = serviceVersionMin != null
		    ? new Version(serviceVersionMin)
		    : null;
		var serviceVersionMaxObject
		    = serviceVersionMax != null
		    ? new Version(serviceVersionMax)
		    : null;
		var isServiceStateSpecified = serviceState.HasValue;
		var serviceStateSpecified = serviceState ?? ServiceState.Offline;
		foreach (var serviceFoundInfo
		    in
		    allServiceFoundInfes)
		{
			if (isGroupNameSpecified
			    && !StringExtension.EqualsIgnoreCase(
			    serviceFoundInfo.GroupName,
			    groupName))
			{
				continue;
			}
			if (isServiceNameSpecified
			    && !StringExtension.EqualsIgnoreCase(
			    serviceFoundInfo.ServiceName,
			    serviceName))
			{
				continue;
			}

			if (serviceVersionMinObject != null
			    || serviceVersionMaxObject != null
			    || isServiceStateSpecified)
			{
				var nodeInfes = serviceFoundInfo.NodeInfes;
				foreach (var nodeInfo in nodeInfes)
				{
					if (serviceVersionMinObject != null
					    && serviceVersionMinObject > nodeInfo.ServiceVersionObject)
					{
						continue;
					}
					if (serviceVersionMaxObject != null
					    && serviceVersionMaxObject < nodeInfo.ServiceVersionObject)
					{
						continue;
					}
					if (isServiceStateSpecified
					    && nodeInfo.ServiceState != serviceStateSpecified)
					{
						continue;
					}
					// !!!
					serviceNodeFoundInfes.Add(nodeInfo);
					// !!!
				}
			}
			else
			{
				serviceNodeFoundInfes.AddRange(serviceFoundInfo.NodeInfes);
			}
		}
		return serviceNodeFoundInfes;
	}

	public ServiceNodeInfo? GetServiceNodeInfoToConnect(
	    string? groupName,
	    string? serviceName,
	    string? serviceVersionMin,
	    string? serviceVersionMax)
	{
		if (string.IsNullOrEmpty(serviceName))
		{
			return null;
		}

		var serviceNodeFoundInfes
		    = GetServiceNodeFoundInfesWithGroupName(
		    groupName,
		    serviceName,
		    serviceVersionMin,
		    serviceVersionMax,
		    ServiceState.InService);
		if (ListUtil.IsEmpty(serviceNodeFoundInfes))
		{
			return null;
		}

		var serviceNodeRequestRateTotal = 0.0;
		ServiceNodeInfo? lastServiceNodeFoundInfo = null;
		foreach (var serviceNodeInfo in serviceNodeFoundInfes)
		{
			var serviceNodeInfoRequestRate = serviceNodeInfo.RequestRate;
			if (serviceNodeInfoRequestRate < 0)
			{
				serviceNodeInfoRequestRate = ServiceNodeInfo.RequestRateDefault;
			}
			serviceNodeRequestRateTotal += serviceNodeInfoRequestRate;
			//
			lastServiceNodeFoundInfo = serviceNodeInfo;
			//
		}

		var serviceNodeRequestRateObject
		    = serviceNodeRequestRateTotal
		    * Random.Shared.NextDouble();
		var serviceNodeRequestRateSectionBegin
		    = 0.0;
		foreach (var serviceNodeInfo in serviceNodeFoundInfes)
		{
			var serviceNodeInfoRequestRate = serviceNodeInfo.RequestRate;
			if (serviceNodeInfoRequestRate < 0)
			{
				serviceNodeInfoRequestRate = ServiceNodeInfo.RequestRateDefault;
			}
			var serviceNodeRequestRateSectionEnd
			    = serviceNodeRequestRateSectionBegin
			    + serviceNodeInfoRequestRate;
			if (serviceNodeRequestRateObject >= serviceNodeRequestRateSectionBegin
			    && serviceNodeRequestRateObject < serviceNodeRequestRateSectionEnd)
			{
				return serviceNodeInfo;
			}
			serviceNodeRequestRateSectionBegin
			    = serviceNodeRequestRateSectionEnd;
		}
		return lastServiceNodeFoundInfo;
	}

	public async Task<ServiceNodeInfo?> GetServiceNodeInfoToConnectAsync(
	    string? groupName,
	    string? serviceName,
	    string? serviceVersionMin,
	    string? serviceVersionMax,
	    //
	    double timeoutSeconds,
	    CancellationToken cancellationToken)
	{
		var serviceNodeInfo = GetServiceNodeInfoToConnect(
		    groupName,
		    serviceName,
		    serviceVersionMin,
		    serviceVersionMax);
		if (serviceNodeInfo != null)
		{
			return serviceNodeInfo;
		}

		var serviceInfo = _serviceInfesNeedFound.GetServiceInfoWithGroupName(groupName, serviceName);
		if (serviceInfo == null)
		{
			return null;
		}

		var taskToDidServiceInfoUpdated
		    = serviceInfo.AddTaskCompletionSourcesToDidServiceInfoUpdated(
		    new());
		if (taskToDidServiceInfoUpdated == null)
		{
			return null;
		}

		var timeoutCancellationTokenSource
		    = timeoutSeconds > 0
		    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
		    : null;
		if (timeoutCancellationTokenSource != null)
		{
			timeoutCancellationTokenSource.Token.Register(() =>
			{
				taskToDidServiceInfoUpdated.TrySetCanceled();
			});

			var timeoutMilliseconds = (int)(1000 * timeoutSeconds);
			timeoutCancellationTokenSource?.CancelAfter(timeoutMilliseconds);
		}

		try
		{
			// !!!
			await taskToDidServiceInfoUpdated.Task;
			// !!!
		}
		catch (TaskCanceledException)
		{
			return null;
		}

		serviceNodeInfo
		    = GetServiceNodeInfoToConnect(
		    groupName,
		    serviceName,
		    serviceVersionMin,
		    serviceVersionMax);
		{ }
		return serviceNodeInfo;
	}

	public ServiceNeedFoundInfo? AddServiceNeedFound(
	    string? serviceGroupName,
	    string? serviceName,
	    Action<ServiceNeedFoundInfo>? toDidServiceInfoUpdated)
	{
		if (string.IsNullOrEmpty(serviceName))
		{
			return null;
		}


		lock (_serviceInfesNeedFound)
		{
			var serviceInfo = _serviceInfesNeedFound.GetServiceInfoWithGroupName(
			    serviceGroupName,
			    serviceName);
			if (serviceInfo == null)
			{
				serviceInfo = new ServiceNeedFoundInfo(
				    serviceGroupName,
				    serviceName);
				{
					_serviceInfesNeedFound
					    = _serviceInfesNeedFound.ArrayByAdd(serviceInfo);
				}
			}
			// !!!
			serviceInfo.AddActionToDidServiceInfoUpdated(
			    toDidServiceInfoUpdated);
			// !!!
			return serviceInfo;
		}
	}

	public void Start(
	    ILogFile? exceptionLogFile,
	    Func<double>? toGetIntervalSecondsToUpdateServiceFoundInfes,
	    bool isServiceNodeInfoStateMissingToUnknowEnable,
	    Func<double>? toGetMissingSecondsToSetServiceNodeInfesStateUnknow)
	{
		_exceptionLogFile = exceptionLogFile;

		ToGetIntervalSecondsToUpdateServiceFoundInfes
		    = toGetIntervalSecondsToUpdateServiceFoundInfes;
		_isServiceNodeInfoStateMissingToUnknowEnable
		     = isServiceNodeInfoStateMissingToUnknowEnable;
		ToGetMissingSecondsToSetServiceNodeInfesStateUnknow
		    = toGetMissingSecondsToSetServiceNodeInfesStateUnknow;

		_taskToUpdateServiceNodeInfes?.Cancel();
		_taskToUpdateServiceNodeInfes = new LoopTask((cancellationToken) =>
		{
			TryProcessServiceNodeInfesNeedUpdated();

			return true;
		},
		() =>
		{
			var intervalSeconds = 0.0;
			if (ToGetIntervalSecondsToUpdateServiceFoundInfes != null)
			{
				intervalSeconds = ToGetIntervalSecondsToUpdateServiceFoundInfes();
			}
			if (intervalSeconds <= 0.0)
			{
				intervalSeconds = IntervalSecondsToUpdateServiceFoundInfesDefault;
			}
			return intervalSeconds;
		});
	}

	#endregion
}
