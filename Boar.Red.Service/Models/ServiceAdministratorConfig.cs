using BaoXia.Utils.Models;
using System.Text.Json.Serialization;

namespace Boar.Red.Service.Models;

public class ServiceAdministratorConfig
{
	////////////////////////////////////////////////
	// @静态常量
	////////////////////////////////////////////////

	#region 静态常量

	/// <summary>
	/// 会话信息保留时长默认值：3600 秒，即 1 小时。
	/// </summary>
	public const double SessionLiveSecondsMaxDefault = 3600;

	/// <summary>
	/// 会话信息同时存在的最大数：小于等于“0”时，不限制。
	/// </summary>
	public const int SessionsCountMaxDefault = 1;

	/// <summary>
	/// 服务节点信息更新间隔秒数默认值：1 秒。
	/// </summary>
	public const double ServiceNodeInfesUpdateIntervalSecondsDefault = 1;

	/// <summary>
	/// 服务节点信息过期至无效状态的秒数默认值：5.0 秒。
	/// </summary>
	public const double ServiceNodeInfesMissingSecondsToInvalidDefault = 5.0;

	#endregion


	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

	public static readonly ServiceAdministratorConfig Default = new();

	#endregion



	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	protected IPAddressInfo[]? _administratorIPAddressInfes;

	[JsonIgnore]
	public IPAddressInfo[]? AdministratorIPAddressInfes
	{
		get
		{
			return _administratorIPAddressInfes;
		}
		set
		{
			_administratorIPAddressInfes = value;
		}
	}

	protected string[]? _administratorIPAddresses;

	/// <summary>
	/// 可进行后台操作的IP白名单，支持“*”号，通配符。
	/// </summary>
	public string[]? AdministratorIPAddresses
	{
		get
		{
			return _administratorIPAddresses;
		}
		set
		{
			_administratorIPAddresses = value;

			if (_administratorIPAddresses?.Length > 0)
			{
				var ipAddressInfoList = new List<IPAddressInfo>();
				foreach (var ipAddress in _administratorIPAddresses)
				{
					if (ipAddress?.Length > 0)
					{
						var ipAddressInfo = new IPAddressInfo(ipAddress);
						{ }
						ipAddressInfoList.Add(ipAddressInfo);
					}
				}
				_administratorIPAddressInfes = [.. ipAddressInfoList];
			}
			else
			{
				_administratorIPAddressInfes = null;
			}
		}
	}

	/// <summary>
	/// 管理员用户信息。
	/// </summary>
	public ServiceAdministrator[]? Administrators { get; set; }

	/// <summary>
	/// 会话信息保留时长，单位为：秒，默认为：3600秒，设置为0时，表示永不过期。
	/// </summary>
	public double SessionRetainSeconds { get; set; } = SessionLiveSecondsMaxDefault;

	/// <summary>
	/// 同一账号，最多会话数。
	/// </summary>
	public int SessionsCountMax { get; set; } = SessionsCountMaxDefault;

	/// <summary>
	/// 服务节点信息更新间隔秒数。
	/// </summary>
	public double ServiceNodeInfesUpdateIntervalSeconds { get; set; } = ServiceNodeInfesUpdateIntervalSecondsDefault;

	/// <summary>
	/// 服务节点信息过期至无效状态的秒数。
	/// </summary>
	public double ServiceNodeInfesMissingSecondsToInvalid { get; set; } = ServiceNodeInfesMissingSecondsToInvalidDefault;

	#endregion
}
