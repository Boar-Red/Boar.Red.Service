using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.ViewModels;

namespace Boar.Red.Service.BrService.Extensions
{
	public static class ServiceInfesExtension
	{
		////////////////////////////////////////////////
		// @类方法
		////////////////////////////////////////////////

		#region 类方法

		public static ServiceInfo? FindServiceInfo(
		    this IList<ServiceInfo> serviceRegisterInfes,
		    string? groupName,
		    string? serviceName)
		{
			for (var serviceFoundInfoIndex = serviceRegisterInfes.Count - 1;
			    serviceFoundInfoIndex >= 0;
			    serviceFoundInfoIndex--)
			{
				var serviceFoundInfo = serviceRegisterInfes[serviceFoundInfoIndex];
				if (StringExtension.EqualsIgnoreCase(serviceFoundInfo.GroupName, groupName)
				    && StringExtension.EqualsIgnoreCase(serviceFoundInfo.ServiceName, serviceName))
				{
					return serviceFoundInfo;
				}
			}
			return null;
		}

		public static ServiceInfo TryAddServiceInfo(
		    this IList<ServiceInfo> serviceNodeInfes,
		    string? serviceGroupName,
		    string? serviceName)
		{
			var serviceFoundInfoExisted = FindServiceInfo(
			    serviceNodeInfes,
			    serviceGroupName,
			    serviceName);
			if (serviceFoundInfoExisted == null)
			{
				serviceFoundInfoExisted = new ServiceInfo(
				    0,
				    serviceGroupName,
				    serviceName);
				serviceNodeInfes.Add(serviceFoundInfoExisted);
			}
			return serviceFoundInfoExisted;
		}

		public static List<ServiceInfo> CloneToUpdateNode(
			this IList<ServiceInfo> serviceFoundInfes, Action<ServiceNodeInfo>? toUpdateNode)
		{
			var serviceFoundInfesCloned = new List<ServiceInfo>();
			foreach (var serviceFoundInfo in serviceFoundInfes)
			{
				var serviceNodeInfoCloned
				    = serviceFoundInfo.CloneToUpdateServiceNodeInfo(
				    toUpdateNode);
				// !!!
				serviceFoundInfesCloned.Add(serviceNodeInfoCloned);
				// !!!
			}
			return serviceFoundInfesCloned;
		}

		#endregion
	}
}
