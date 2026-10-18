using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.ViewModels;

namespace Boar.Red.Service.BrService.Extensions
{
	public static class ServiceNeedFoundInfesExtension
	{
		////////////////////////////////////////////////
		// @类方法
		////////////////////////////////////////////////

		#region 类方法

		public static ServiceNeedFoundInfo? GetServiceInfoWithGroupName(
		    this IEnumerable<ServiceNeedFoundInfo> serviceInfes,
		    string? groupName,
		    string? serviceName)
		{
			if (string.IsNullOrEmpty(serviceName))
			{
				return null;
			}

			var isGroupNameEmpty = string.IsNullOrEmpty(groupName);
			foreach (var serviceInfo in serviceInfes)
			{
				if (isGroupNameEmpty
				    || StringExtension.EqualsIgnoreCase(
				    serviceInfo.GroupName,
				    groupName))
				{
					if (StringExtension.EqualsIgnoreCase(
					    serviceInfo.ServiceName,
					    serviceName))
					{
						return serviceInfo;
					}
				}
			}
			return null;
		}

		#endregion
	}
}
