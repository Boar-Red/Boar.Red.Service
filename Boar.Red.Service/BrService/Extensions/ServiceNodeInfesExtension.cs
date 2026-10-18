using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.Constants;
using Boar.Red.Service.BrService.ViewModels;

namespace Boar.Red.Service.BrService.Extensions
{
	public static class ServiceNodeInfesExtension
	{
		////////////////////////////////////////////////
		// @类方法
		////////////////////////////////////////////////

		#region 类方法

		public static ServiceNodeInfo? FindServiceNodeInfo(
		    this IList<ServiceNodeInfo> serviceNodeInfes,
		    string? groupName,
		    string? serviceName,
		    string? nodeName)
		{
			for (var serviceNodeInfoIndex = serviceNodeInfes.Count - 1;
			    serviceNodeInfoIndex >= 0;
			    serviceNodeInfoIndex--)
			{
				var serviceNodeInfo = serviceNodeInfes[serviceNodeInfoIndex];
				if (StringExtension.EqualsIgnoreCase(serviceNodeInfo.GroupName, groupName)
				    && StringExtension.EqualsIgnoreCase(serviceNodeInfo.ServiceName, serviceName)
				    && StringExtension.EqualsIgnoreCase(serviceNodeInfo.NodeName, nodeName))
				{
					return serviceNodeInfo;
				}
			}
			return null;
		}

		public static ServiceNodeInfo AddOrUpdateServiceNodeInfo(
		    this IList<ServiceNodeInfo> serviceNodeInfes,
		    ServiceNodeInfo serviceNodeInfo,
		    out ItemFoundType serviceNodeInfoFoundType)
		{
			serviceNodeInfoFoundType = ItemFoundType.NormalFound;

			var serviceNodeInfoExisted = serviceNodeInfes.FindServiceNodeInfo(
			    serviceNodeInfo.GroupName, serviceNodeInfo.ServiceName, serviceNodeInfo.NodeName);
			if (serviceNodeInfoExisted != null)
			{
				if (serviceNodeInfoExisted.IsDifferentServiceInfo(serviceNodeInfo))
				{
					// !!!
					serviceNodeInfoFoundType = ItemFoundType.UpdatedItemFound;
					// !!!
				}
				serviceNodeInfoExisted.SetPropertiesWithSameNameFrom(serviceNodeInfo, nameof(ServiceNodeInfo.Id));
			}
			else
			{
				// !!!
				serviceNodeInfoFoundType = ItemFoundType.NewItemFound;
				// !!!
				serviceNodeInfoExisted = serviceNodeInfo;
				serviceNodeInfes.Add(serviceNodeInfo);
			}
			return serviceNodeInfoExisted;
		}

		#endregion
	}
}
