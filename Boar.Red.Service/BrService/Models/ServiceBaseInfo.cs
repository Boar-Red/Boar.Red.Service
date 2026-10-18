namespace Boar.Red.Service.BrService.Models;

public class ServiceBaseInfo
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	/// <summary>
	/// 服务分组名称。
	/// </summary>
	public string? GroupName { get; set; }

	/// <summary>
	/// 服务名称。
	/// </summary>
	public string? ServiceName { get; set; }

	/// <summary>
	/// 服务版本号。
	/// </summary>
	public string? ServiceVersion { get; set; }

	/// <summary>
	/// 服务更新时间。
	/// </summary>
	public DateTime ServiceUpdateTime { get; set; }

	/// <summary>
	/// 节点名称。
	/// </summary>
	public string? NodeName { get; set; }

	/// <summary>
	/// 服务信息描述。
	/// </summary>
	public string? ServiceDescription { get; set; }

	/// <summary>
	/// Http协议绑定的IP地址。
	/// </summary>
	public string? HttpIPAddress { get; set; }

	/// <summary>
	/// Https协议绑定的IP地址。
	/// </summary>
	public string? HttpsIPAddress { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ServiceBaseInfo(
	    string? groupName,
	    string? serviceName,
	    string? nodeName,
	    string? serviceDescription)
	{
		GroupName = groupName;
		ServiceName = serviceName;
		NodeName = nodeName;
		ServiceDescription = serviceDescription;
	}

	#endregion
}
