using BaoXia.Utils;
using Boar.Red.Service.BrService.Models.StatisticsInfo;

namespace Boar.Red.Service.BrService.ViewModels;

public class ServiceInitializationParam
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性
	public ConfigFile? WebServiceConfigFile { get; set; }
	public string? WebServiceConfigPropertyNameInConfigFile { get; set; }

	public bool IsWebServiceEnable
	{
		get
		{
			var isWebServiceEnable
			= WebServiceConfigFile != null
			&& string.IsNullOrEmpty(WebServiceConfigPropertyNameInConfigFile) != true;
			{ }
			return isWebServiceEnable;
		}
	}

	public ConfigFile? BrServiceConfigFile { get; set; }
	public string? BrServiceConfigPropertyNameInConfigFile { get; set; }

	public bool IsBrServiceEnable
	{
		get
		{
			var isBrServiceEnable = BrServiceConfigFile != null
			&& string.IsNullOrEmpty(BrServiceConfigPropertyNameInConfigFile) != true;
			{ }
			return isBrServiceEnable;
		}
	}

	public bool IsControllerBrServiceDefault { get; set; } = true;
	public bool IsHttpRequestDisable { get; set; }
	public bool IsWebSocketEnable { get; set; }

	public ServiceDiscoverer? ServiceDiscoverSpecified { get; set; }

	public Action<Action<string, double>, CancellationToken>? ToServiceBooting { get; set; }
	public Action<CancellationToken>? ToServiceStarted { get; set; }
	public Action<CancellationToken>? ToServiceShutdown { get; set; }
	public Func<string?, bool>? ToIsClientVersionSupported { get; set; }

	public Func<double>? ToGetServiceLoadRate { get; set; }

	public Action<ServiceRequestStatisticsInfo[]>? ToSaveServiceRequestStatisticsInfes { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	/// <summary>
	/// Boar.Red 服务初始化参数。
	/// </summary>
	/// <param name="webServiceConfigFile">Web服务配置所在的配置文件对象。</param>
	/// <param name="webServiceConfigPropertyNameInConfigFile">Web服务配置所在的配置文件对象中的属性名称。</param>
	/// <param name="bxServiceConfigFile">Boar.Red 服务配置所在的配置文件对象。</param>
	/// <param name="bxServiceConfigPropertyNameInConfigFile">Boar.Red 服务配置所在的配置文件对象中的属性名称。</param>
	/// <param name="isControllerBrServiceDefault">是否所有控制器，都默认同时是长连接服务的控制器。</param>
	/// <param name="isHttpRequestDisable">是否禁用Http请求。</param>
	/// <param name="isWebSocketEnable">是否开启WebSocket服务。</param>
	/// <param name="serviceDiscoverSpecified">指定的服务发现器。</param>
	/// <param name="toServiceBooting">事件节点，服务启动中。</param>
	/// <param name="toServiceStarted">事件节点，服务已启动。</param>
	/// <param name="toServiceShutdown">事件节点，服务关机中。</param>
	/// <param name="toIsClientVersionSupported">事件节点，是否为支持的客户端版本。</param>
	/// <param name="toGetServiceLoadRate">事件节点，获取自定义的服务负载率。</param>
	/// <param name="toSaveServiceRequestStatisticsInfes">事件节点，保存服务的统计信息。</param>
#pragma warning disable IDE0290 // 使用主构造函数
	public ServiceInitializationParam(
#pragma warning restore IDE0290 // 使用主构造函数
	    ConfigFile? webServiceConfigFile,
	    string? webServiceConfigPropertyNameInConfigFile,
	    ConfigFile? bxServiceConfigFile,
	    string? bxServiceConfigPropertyNameInConfigFile,
	    bool isControllerBrServiceDefault = true,
	    bool isHttpRequestDisable = false,
	    bool isWebSocketEnable = false,
	    ServiceDiscoverer? serviceDiscoverSpecified = null,
	    Action<Action<string, double>, CancellationToken>? toServiceBooting = null,
	    Action<CancellationToken>? toServiceStarted = null,
	    Action<CancellationToken>? toServiceShutdown = null,
	    Func<string?, bool>? toIsClientVersionSupported = null,
	    Func<double>? toGetServiceLoadRate = null,
	    Action<ServiceRequestStatisticsInfo[]>? toSaveServiceRequestStatisticsInfes = null)
	{
		WebServiceConfigFile = webServiceConfigFile;
		WebServiceConfigPropertyNameInConfigFile = webServiceConfigPropertyNameInConfigFile;
		BrServiceConfigFile = bxServiceConfigFile;
		BrServiceConfigPropertyNameInConfigFile = bxServiceConfigPropertyNameInConfigFile;

		IsControllerBrServiceDefault = isControllerBrServiceDefault;
		IsHttpRequestDisable = isHttpRequestDisable;
		IsWebSocketEnable = isWebSocketEnable;
		ServiceDiscoverSpecified = serviceDiscoverSpecified;
		ToServiceBooting = toServiceBooting;
		ToServiceStarted = toServiceStarted;
		ToServiceShutdown = toServiceShutdown;
		ToIsClientVersionSupported = toIsClientVersionSupported;

		ToGetServiceLoadRate = toGetServiceLoadRate;
		ToSaveServiceRequestStatisticsInfes = toSaveServiceRequestStatisticsInfes;
	}

	#endregion
}
