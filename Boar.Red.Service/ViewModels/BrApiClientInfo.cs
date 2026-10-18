namespace Boar.Red.Service.ViewModels;

public class BrApiClientInfo
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string? ServiceProviderClientIdentity { get; set; }

	public string? Version { get; set; }

	public string? InstallChannel { get; set; }

	public string? OSName { get; set; }

	public string? OSVersion { get; set; }

	public string? DeviceName { get; set; }

	public string? DeviceId { get; set; }


	/// <summary>
	/// 连接的Ip终结点集合字符串，含端口号。
	/// </summary>
	public string? ConnectionIpEndPointsString { get; set; }

	/// <summary>
	/// 关键Ip地址，不含端口号。
	/// </summary>
	public string? KeyIpAddress { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public BrApiClientInfo()
	{ }

	public BrApiClientInfo(
	    string? serviceProviderClientIdentity,
	    string? version,
	    //
	    string? installChannel,
	    //
	    string? osName,
	    string? osVersion,
	    //
	    string? deviceName,
	    string? deviceId,
	    //
	    string? connectionIpEndPointsString,
	    string? keyIpAddress)
	{
		ServiceProviderClientIdentity = serviceProviderClientIdentity;
		Version = version;
		InstallChannel = installChannel;
		OSName = osName;
		OSVersion = osVersion;
		DeviceName = deviceName;
		DeviceId = deviceId;

		ConnectionIpEndPointsString = connectionIpEndPointsString;
		KeyIpAddress = keyIpAddress;
	}

	#endregion
}