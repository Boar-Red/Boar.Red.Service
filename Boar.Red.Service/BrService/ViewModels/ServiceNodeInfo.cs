using BaoXia.Utils;
using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.Constants;
using System.Net;
using System.Text.Json.Serialization;

namespace Boar.Red.Service.BrService.ViewModels;

public class ServiceNodeInfo : UdpDataPacket
{

	////////////////////////////////////////////////
	// @静态常量
	////////////////////////////////////////////////

	#region 静态常量

	public const double RequestRateDefault = 1.0;

	#endregion


	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public int Id { get; set; }

	public ServiceNodeType NodeType { get; set; }

	public string? GroupName { get; set; }

	public string? ServiceName { get; set; }

	public string? NodeName { get; set; }

	protected string? _serviceVersion;
	protected Version? _serviceVersionObject;

	public string? ServiceVersion
	{
		get
		{
			return _serviceVersion;
		}
		set
		{
			if (StringUtil.EqualsStrings(
			    _serviceVersion,
			    value))
			{
				return;
			}

			_serviceVersion = value;
			_serviceVersionObject = _serviceVersion != null
			    ? new Version(_serviceVersion)
			    : null;
		}
	}

	[JsonIgnore]
	public Version? ServiceVersionObject => _serviceVersionObject;

	public string? ServiceAddress { get; set; }

	public int ServicePort { get; set; }

	public string? HttpServiceUri
	{
		get
		{
			var serviceAddress = ServiceAddress;
			if (string.IsNullOrEmpty(serviceAddress))
			{
				return null;
			}

			var serviceHttpAddress = "http://" + serviceAddress;
			var servicePort = ServicePort;
			if (servicePort != 80 && servicePort != 0)
			{
				serviceHttpAddress += ":" + servicePort;
			}
			return serviceHttpAddress;
		}
	}

	public string? ServiceAddressInHttps { get; set; }

	public int ServicePortInHttps { get; set; }

	public string? HttpsServiceUri
	{
		get
		{
			var serviceAddress = ServiceAddressInHttps;
			if (string.IsNullOrEmpty(serviceAddress))
			{
				return null;
			}

			var serviceHttpsAddress = "https://" + serviceAddress;
			var servicePort = ServicePortInHttps;
			if (servicePort != 443)
			{
				serviceHttpsAddress += ":" + servicePort;
			}
			return serviceHttpsAddress;
		}
	}

	public string? BrServiceUri
	{
		get
		{
			var serviceAddress = ServiceAddress;
			if (string.IsNullOrEmpty(serviceAddress))
			{
				return null;
			}

			var serviceHttpAddress = "ws://" + serviceAddress;
			var servicePort = ServicePort;
			if (servicePort != 80 && servicePort != 0)
			{
				serviceHttpAddress += ":" + servicePort;
			}
			return serviceHttpAddress;
		}
	}

	public string? BrServiceUriInHttps
	{
		get
		{
			var serviceAddress = ServiceAddressInHttps;
			if (string.IsNullOrEmpty(serviceAddress))
			{
				return null;
			}

			var serviceHttpsAddress = "wss://" + serviceAddress;
			var servicePort = ServicePortInHttps;
			if (servicePort != 443)
			{
				serviceHttpsAddress += ":" + servicePort;
			}
			return serviceHttpsAddress;
		}
	}

	public string? FinalHttpUri
	{
		get
		{
			if (HttpServiceUri is string serviceHttpAddress)
			{
				return serviceHttpAddress;
			}
			else if (HttpsServiceUri is string serviceHttpsAddress)
			{
				return serviceHttpsAddress;
			}
			return null;
		}
	}

	public string? FinalBrServiceUri
	{
		get
		{
			if (BrServiceUri is string bxServiceUri)
			{
				return bxServiceUri;
			}
			else if (BrServiceUriInHttps is string bxServiceUriInHttps)
			{
				return bxServiceUriInHttps;
			}
			return null;
		}
	}

	public int ServiceDiscovererListenPort { get; set; }

	[JsonIgnore]
	public IPEndPoint? ServiceDiscovererEndPoint { get; set; }

	public ServiceState ServiceState { get; set; }

	public bool IsEnable
	{
		get
		{
			switch (ServiceState)
			{
				default:
				case ServiceState.Offline:
				case ServiceState.InBooting:
				case ServiceState.InShuttingDown:
				case ServiceState.InShutdown:
					break;
				case ServiceState.InService:
					{
						return true;
					}
			}
			return false;
		}
	}

	public bool IsDisable
	{
		get
		{
			return !IsEnable;
		}
	}

	public string? ServiceStateName
	{
		get
		{
			return EnumUtil.NameOf(ServiceState);
		}
	}

	public DateTime StartTime { get; set; }

	/// <summary>
	/// 服务运行的持续时间。
	/// </summary>
	//[JsonIgnore]
	public TimeSpan StartDuration
	{
		get
		{
			var serviceStartDuration = DateTime.Now - StartTime;
			{ }
			return serviceStartDuration;
		}
	}

	public double RequestRate { get; set; } = RequestRateDefault;

	public double LoadRate { get; set; }

	/// <summary>
	/// 信息同步命令。
	/// </summary>
	public int SyncCommand { get; set; } = ServiceNodeInfoSyncCommand.None;

	public bool IsValid
	{
		get
		{
			if (ServiceName?.Length > 0 && ServicePort >= 0)
			{
				return true;
			}
			return false;
		}
	}

	public DateTime CreateTime { get; set; }

	public DateTime UpdateTime { get; set; }

	public DateTime ServiceInfoChangeTime { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ServiceNodeInfo()
	{ }

	public ServiceNodeInfo(
	    ServiceNodeType nodeType,
	    string? groupName,
	    string? serviceName,
	    string? nodeName,
	    string? serviceVersion,
	    //
	    string? serviceAddress,
	    int servicePort,
	    string? serviceAddressInHttps,
	    int servicePortInHttps,
	    int serviceDiscovererListenPort,
	    IPEndPoint? serviceDiscovererEndPoint,
	    //
	    ServiceState serviceState,
	    DateTime startTime,
	    //
	    double loadRate,
	    double requestRate = RequestRateDefault)
	{
		NodeType = nodeType;
		GroupName = groupName;
		ServiceName = serviceName;
		NodeName = nodeName;
		ServiceVersion = serviceVersion;

		ServiceAddress = serviceAddress;
		ServicePort = servicePort;
		ServiceAddressInHttps = serviceAddressInHttps;
		ServicePortInHttps = servicePortInHttps;
		ServiceDiscovererListenPort = serviceDiscovererListenPort;
		ServiceDiscovererEndPoint = serviceDiscovererEndPoint;

		ServiceState = serviceState;
		StartTime = startTime;
		RequestRate = requestRate;
		LoadRate = loadRate;
	}

	public bool IsSameGroup(string? serviceGroupName)
	{
		if (StringExtension.EqualsIgnoreCase(
		    GroupName,
		    serviceGroupName)
		    == true)
		{
			return true;
		}
		return false;
	}

	public bool IsSameGroup(ServiceNodeInfo? serviceNodeInfo)
	{
		if (serviceNodeInfo == null)
		{
			return false;
		}
		return IsSameGroup(serviceNodeInfo.GroupName);
	}

	public bool IsSameService(
	    string? serviceGroupName,
	    string? serviceName)
	{
		if (!IsSameGroup(serviceGroupName))
		{
			return false;
		}

		if (StringExtension.EqualsIgnoreCase(ServiceName, serviceName) == true)
		{
			return true;
		}
		return false;
	}

	public bool IsSameService(ServiceNodeInfo? serviceNodeInfo)
	{
		if (serviceNodeInfo == null)
		{
			return false;
		}
		return IsSameService(
		    serviceNodeInfo.GroupName,
		    serviceNodeInfo.ServiceName);
	}

	public bool IsSameServiceNode(
	    string? groupName,
	    string? serviceName,
	    string? nodeName)
	{
		if (!IsSameService(
		    groupName,
		    serviceName))
		{
			return false;
		}

		if (StringExtension.EqualsIgnoreCase(
		    NodeName,
		    nodeName)
		    == true)
		{
			return true;
		}
		return false;
	}

	public bool IsSameServiceNode(ServiceNodeInfo? serviceNodeInfo)
	{
		if (serviceNodeInfo == null)
		{
			return false;
		}

		return IsSameServiceNode(
		    serviceNodeInfo.GroupName,
		    serviceNodeInfo.ServiceName,
		    serviceNodeInfo.NodeName);
	}

	public bool IsSameServiceInfo(ServiceNodeInfo serviceNodeInfo)
	{
		// 版本不同：
		if (!StringUtil.EqualsStrings(ServiceVersion, serviceNodeInfo.ServiceVersion))
		{
			return false;
		}
		// 服务状态不同：
		if (ServiceState != serviceNodeInfo.ServiceState)
		{
			return false;
		}
		// Http服务地址不同：
		if (!StringUtil.EqualsStrings(ServiceAddress, serviceNodeInfo.ServiceAddress))
		{
			return false;
		}
		var finalServicePort = ServicePort == 0 ? 80 : ServicePort;
		var finalAnotherServicePort = serviceNodeInfo.ServicePort == 0 ? 80 : serviceNodeInfo.ServicePort;
		if (finalServicePort != finalAnotherServicePort)
		{
			return false;
		}
		// Https服务地址不同：
		if (!StringUtil.EqualsStrings(ServiceAddressInHttps, serviceNodeInfo.ServiceAddressInHttps))
		{
			return false;
		}
		var finalServicePortInHttps = ServicePortInHttps == 0 ? 443 : ServicePortInHttps;
		var finalAnotherServicePortInHttps = serviceNodeInfo.ServicePortInHttps == 0 ? 443 : serviceNodeInfo.ServicePortInHttps;
		if (finalServicePortInHttps != finalAnotherServicePortInHttps)
		{
			return false;
		}
		// 请求比例不同：
		if (RequestRate != serviceNodeInfo.RequestRate)
		{
			return false;
		}

		return true;
	}

	public bool IsDifferentServiceInfo(ServiceNodeInfo serviceNodeInfo)
	{
		return !IsSameServiceInfo(serviceNodeInfo);
	}

	#endregion


	////////////////////////////////////////////////
	// @实现“Object”
	////////////////////////////////////////////////

	#region 实现“Object”

	public override bool Equals(object? obj)
	{
		if (obj is ServiceNodeInfo anotherServiceIdentityInfo)
		{
			if (IsSameServiceNode(anotherServiceIdentityInfo))
			{
				return true;
			}
		}
		return false;
	}

	public override int GetHashCode()
	{
		var hashString
		    = (GroupName ?? string.Empty)
		    + "\r\n"
		    + (ServiceName ?? string.Empty)
		    + "\r\n"
		    + (NodeName ?? string.Empty);
		var hashCode = hashString.GetHashCode();
		{ }
		return hashCode;
	}

	#endregion


	////////////////////////////////////////////////
	// @实现“UdpDataPacket”
	////////////////////////////////////////////////

	#region 实现“UdpDataPacket”

	public override string DataPacketName => UdpDataPackageNames.BrService_UdpDataPackage_ServiceNodeInfo;

	protected override void DidWriteDataBytes(ref BytesOperator bytesOperator)
	{
		bytesOperator.Write((byte)NodeType);
		bytesOperator.Write(GroupName);
		bytesOperator.Write(ServiceName);
		bytesOperator.Write(NodeName);
		bytesOperator.Write(ServiceVersion);

		bytesOperator.Write(ServiceAddress);
		bytesOperator.Write(ServicePort);
		bytesOperator.Write(ServiceAddressInHttps);
		bytesOperator.Write(ServicePortInHttps);
		bytesOperator.Write(ServiceDiscovererListenPort);

		bytesOperator.Write((byte)ServiceState);
		bytesOperator.Write(RequestRate);
		bytesOperator.Write(LoadRate);
		bytesOperator.Write(SyncCommand);
	}

	protected override void DidReadDataBytes(ref BytesOperator bytesOperator)
	{
		NodeType = (ServiceNodeType)bytesOperator.ReadByte();
		GroupName = bytesOperator.ReadString();
		ServiceName = bytesOperator.ReadString();
		NodeName = bytesOperator.ReadString();
		ServiceVersion = bytesOperator.ReadString();

		ServiceAddress = bytesOperator.ReadString();
		ServicePort = bytesOperator.ReadInt();
		ServiceAddressInHttps = bytesOperator.ReadString();
		ServicePortInHttps = bytesOperator.ReadInt();
		ServiceDiscovererListenPort = bytesOperator.ReadInt();

		ServiceState = (ServiceState)bytesOperator.ReadByte();
		RequestRate = bytesOperator.ReadDouble();
		LoadRate = bytesOperator.ReadDouble();
		SyncCommand = bytesOperator.ReadInt();
	}

	#endregion
}