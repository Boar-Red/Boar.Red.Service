using BaoXia.Utils;
using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.Constants;
using System.Text.Json.Serialization;

namespace Boar.Red.Service.BrService.Models;

public class ServiceConfig
{
	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

	public static readonly ServiceConfig Default = new();

	#endregion


	////////////////////////////////////////////////
	// @自身属性，服务基础参数
	////////////////////////////////////////////////

	#region 自身属性，服务基础参数

	/// <summary>
	/// 服务基本信息。
	/// </summary>
	public ServiceBaseInfo? ServiceInfo { get; set; }

	/// <summary>
	/// 服务发现配置。
	/// </summary>
	public ServiceDiscovererConfig? ServiceDiscovererConfig { get; set; }

	/// <summary>
	/// 会话登录的超时秒数，建立连接后，如果在指定的秒数内没有进行登录，则中断连接。
	/// </summary>
	public double ClientConnectionOpenTimeoutSeconds { get; set; } = 1.0;

	/// <summary>
	/// 应用程序请求接收缓冲区长度，单位为：字节。
	/// </summary>
	public int ApplicationRequestReceiveBufferSizeInByte { get; set; } = 32 * 1024;

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
	/// 服务关机的超时秒数，如果在指定的秒数内没有安全关机，则进行强制关机，默认为：10 秒。
	/// </summary>
	public double ServiceShutdownTimeoutSeconds { get; set; } = 10.0;

	/// <summary>
	/// Socket连接心跳包的间隔秒数，间隔时间是指当前与业务数据的发送&接收时间的间隔。
	/// </summary>
	public double SocketConnectionHeartbeatIntervalSeconds { get; set; } = 0.0;

	/// <summary>
	/// Socket连接心跳包的最终间隔秒数，系统会根据配置和当前实际网络发送情况计算出合理的最终间隔秒数。
	/// </summary>
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
	/// 请求处理线程数最大值，“0”为不限制。
	/// </summary>
	public int RequestProcessThreadsCountMax { get; set; } = 0;

	/// <summary>
	/// 响应发送异常的处理方式，默认为：Ignore，忽略。
	/// </summary>
	[JsonIgnore]
	public ApiResponseSendExceptionAction ResponseSendExceptionAction { get; set; } = ApiResponseSendExceptionAction.Ignore;

	/// <summary>
	/// 响应发送异常的处理方式名称，默认为：“Ignore”，忽略。
	/// </summary>
	public string? ResponseSendExceptionActionName
	{
		get
		{
			return ResponseSendExceptionAction.Name();
		}
		set
		{
			ResponseSendExceptionAction = EnumUtil.ValueOf(value, ApiResponseSendExceptionAction.Ignore);
		}
	}

	/// <summary>
	/// 客户端发送请求时，数据包进行压缩的最小字节数，“-1”为永不压缩，"0"为无设置。
	/// </summary>
	public int ClientRequestDataPacketSizeToCompression { get; set; }

	/// <summary>
	/// 客户端发送请求时，数据包的压缩算法名称，默认为：“Brotli”。
	/// </summary>
	public string? ClientRequestDataPacketCompressionMethodName { get; set; }

	/// <summary>
	/// 服务端发送请求时，数据包进行压缩的最小字节数，“-1”为永不压缩，"0"为无设置。
	/// </summary>
	public int ServiceResponseDataPacketSizeToCompression { get; set; }

	/// <summary>
	/// 服务端发送请求时，数据包的压缩算法名称，默认为：“Brotli”。
	/// </summary>
	public string? ServiceResponseDataPacketCompressionMethodName { get; set; }

	/// <summary>
	/// 请求统计的保存时长，单位为：秒数，“0”为不收集保留统计信息。
	/// </summary>
	public double RequestStatisticsInLiveSeconds { get; set; }

	/// <summary>
	/// 服务设计的满载RPS值，用于默认的负载率计算。
	/// </summary>
	public double RPSForFullLoadRate { get; set; }

	/// <summary>
	/// 内置Http客户端的请求超时秒数，默认为：1.0 秒。
	/// </summary>
	public double HttpClientRequestTimeoutSeconds { get; set; } = 1.0;

	#endregion


	////////////////////////////////////////////////
	// @自身实现，应用程序配置
	////////////////////////////////////////////////

	#region 自身属性，应用程序配置

	public ApplicationInfo[]? ApplicationInfes { get; set; }

	#endregion

}
