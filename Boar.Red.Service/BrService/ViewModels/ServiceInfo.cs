using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.Constants;

namespace Boar.Red.Service.BrService.ViewModels;

public class ServiceInfo
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public int Id { get; set; }

	public string? GroupName { get; set; }

	public string? ServiceName { get; set; }

	public string? ServiceTitle { get; set; }

	public ServiceState ServiceState { get; set; }

	public double LoadRate { get; set; }

	public ServiceNodeInfo[] NodeInfes { get; set; } = [];

	public DateTime CreateTime { get; set; }

	public DateTime UpdateTime { get; set; }

	public DateTime ServiceInfoChangeTime { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ServiceInfo()
	{ }

	public ServiceInfo(
	    int id = 0,
	    string? groupName = null,
	    string? serviceName = null)
	{
		Id = id;
		GroupName = groupName;
		ServiceName = serviceName;
	}

	public ServiceInfo(ServiceNodeInfo? serviceNodeInfo)
	{
		if (serviceNodeInfo == null)
		{
			return;
		}

		GroupName = serviceNodeInfo.GroupName;
		ServiceName = serviceNodeInfo.ServiceName;
	}

	protected int GetIndexOfServiceNodeInfo(ServiceNodeInfo? serviceNodeInfo)
	{
		if (serviceNodeInfo == null)
		{
			return -1;
		}

		lock (this)
		{
			var nodeInfes = NodeInfes;
			for (var nodeInfoIndex = 0;
			    nodeInfoIndex < nodeInfes.Length;
			    nodeInfoIndex++)
			{
				var nodeInfo = nodeInfes[nodeInfoIndex];
				if (nodeInfo == serviceNodeInfo
				    && nodeInfo.Equals(serviceNodeInfo))
				{
					// !!!
					return nodeInfoIndex;
					// !!!
				}
			}
		}
		return -1;
	}

	public void AddOrUpdateServiceNodeInfo(ServiceNodeInfo? serviceNodeInfo)
	{
		if (serviceNodeInfo == null)
		{
			return;
		}

		lock (this)
		{
			var nodeInfes = NodeInfes;
			var indexOfServiceNodeInfo = GetIndexOfServiceNodeInfo(serviceNodeInfo);
			if (indexOfServiceNodeInfo < 0)
			{
				var serviceNodeInfoList = new List<ServiceNodeInfo>(nodeInfes);
				{
					serviceNodeInfoList.Add(serviceNodeInfo);
					serviceNodeInfoList.Sort((serviceNodeInfoA, serviceNodeInfoB) =>
					{
						var compareResult = string.Compare(
		    serviceNodeInfoA.NodeName,
		    serviceNodeInfoB.NodeName);
						{ }
						return compareResult;
					});
				}
				nodeInfes = [.. serviceNodeInfoList];
			}
			else
			{
				nodeInfes[indexOfServiceNodeInfo] = serviceNodeInfo;
			}
			NodeInfes = nodeInfes;
		}
	}

	public void RemoveServiceNodeInfo(ServiceNodeInfo? serviceNodeInfo)
	{
		if (serviceNodeInfo == null)
		{
			return;
		}

		lock (this)
		{
			var nodeInfoIndexExisted = GetIndexOfServiceNodeInfo(serviceNodeInfo);
			if (nodeInfoIndexExisted < 0)
			{
				return;
			}

			// !!!
			NodeInfes = NodeInfes.ArrayByRemoveAt(nodeInfoIndexExisted);
			// !!!
		}
	}

	public ServiceInfo CloneToUpdateServiceNodeInfo(
	    Action<ServiceNodeInfo>? toUpdateNode)
	{
		var serviceFoundInfoCloned = this.Clone();
		var orignalServiceNodeInfes = serviceFoundInfoCloned.NodeInfes;
		if (orignalServiceNodeInfes != null)
		{
			var serviceNodeInfes
			    = new ServiceNodeInfo[orignalServiceNodeInfes.Length];
			for (var serviceNodeInfoIndex = orignalServiceNodeInfes.Length - 1;
			    serviceNodeInfoIndex >= 0;
			    serviceNodeInfoIndex--)
			{
				var serviceNodeInfo
				    = orignalServiceNodeInfes[serviceNodeInfoIndex]
				    .Clone();
				{
					toUpdateNode?.Invoke(serviceNodeInfo);
				}
				serviceNodeInfes[serviceNodeInfoIndex]
				    = serviceNodeInfo;
			}
			serviceFoundInfoCloned.NodeInfes = serviceNodeInfes;
		}
		return serviceFoundInfoCloned;
	}

	#endregion
}
