using BaoXia.Utils;
using Boar.Red.Service.BrService.Constants;
using System.Text.Json.Serialization;

namespace Boar.Red.Service.BrService.Models;

/// <summary>
/// 客户端配置信息对象。
/// </summary>
public class ClientConfig
{
	////////////////////////////////////////////////
	// @静态常量
	////////////////////////////////////////////////

	#region 静态常量

	public const int ResponseProcessThreadsCountMaxDefault = 32;

	#endregion


	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

	public static readonly ClientConfig Default = new();

	#endregion


	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string ServiceUri { get; set; } = default!;

	public string? ServiceAdministratorAccount { get; set; }
	public string? ServiceAdministratorPassword { get; set; }


	public string ApplicationName { get; set; } = default!;

	public string ApplicationIdentity { get; set; } = default!;

	public string ApplicationPassword { get; set; } = default!;

	public string ApplicationAesKey { get; set; } = default!;


	public int ApiResponseReceiveBufferSizeInByte { get; set; }

	public double ClientConnectionOpenTimeoutSeconds { get; set; }

	/// <summary>
	/// 客户端发送请求时，数据包进行压缩的最小字节数，“-1”为永不压缩，"0"为无设置。
	/// </summary>
	public int ClientRequestDataPacketSizeToCompression { get; set; }

	// 客户端发送请求时，数据包的压缩算法名称，默认为：“Brotli”。
	public string? ClientRequestDataPacketCompressionMethodName { get; set; }


	/// <summary>
	/// 请求数据默认编码名称，可选值为：ASCII，UTF-8，UTF-32，默认为： UTF-8。
	/// </summary>
	public string? ClientRequestTextEncodingName { get; set; } = "UTF-8";

	/// <summary>
	/// 请求数据默认编码。
	/// </summary>
	[JsonIgnore]
	public System.Text.Encoding ClientRequestTextEncoding
	{
		get
		{
			return TextEncodingUtil.EncodingNamed(
			    ClientRequestTextEncodingName,
			    DataPacketTextEncoding.Default);
		}
	}

	/// <summary>
	/// 响应数据编码名称，可选值为：ASCII，UTF-8，UTF-32，默认为： UTF-8。
	/// </summary>
	public string? ServiceResponseTextEncodingName { get; set; } = "UTF-8";

	/// <summary>
	/// 响应数据默认编码。
	/// </summary>
	[JsonIgnore]
	public System.Text.Encoding ServiceResponseTextEncoding
	{
		get
		{
			return TextEncodingUtil.EncodingNamed(
			    ServiceResponseTextEncodingName,
			    DataPacketTextEncoding.Default);
		}
	}

	/// <summary>
	/// Socket连接心跳包的间隔秒数，间隔时间是指当前与业务数据的发送&接收时间的间隔。
	/// </summary>
	public double SocketConnectionHeartbeatIntervalSeconds { get; set; } = 0.0;

	[JsonIgnore]
	public double SocketConnectionHeartbeatIntervalSecondsFinally
	{
		get
		{
			var intervalSeconds = SocketConnectionHeartbeatIntervalSeconds;
			if (intervalSeconds
			    < TimerIntervalSeconds.SocketConnectionHeartbeatIntervalSecondsMin)
			{
				intervalSeconds
				= TimerIntervalSeconds.SocketConnectionHeartbeatIntervalSecondsMin;
			}
			return intervalSeconds;
		}
	}

	/// <summary>
	/// 响应处理线程的最大数量，默认为：0，不限制。
	/// </summary>
	public int ResponseProcessThreadsCountMax { get; set; }

	/// <summary>
	/// 请求统计的保存时长，单位为：秒数，“0”为不收集保留统计信息。
	/// </summary>
	public double RequestStatisticsInLiveSeconds { get; set; }

	#endregion
}