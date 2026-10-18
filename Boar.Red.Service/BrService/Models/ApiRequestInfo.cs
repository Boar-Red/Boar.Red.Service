using BaoXia.Utils.Extensions;
using BaoXia.Utils.Models;
using Boar.Red.Service.BrService.Models.StatisticsInfo;
using System.Net.WebSockets;
using System.Text.Json.Serialization;

namespace Boar.Red.Service.BrService.Models;

public class ApiRequestInfo<MethodParamsType>
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	/// <summary>
	/// 当前Socket的接收结果。
	/// </summary>
	[JsonIgnore]
	public WebSocketReceiveResult? WebSocketReceiveResult { get; set; }

	/// <summary>
	/// 当前发送请求的客户端Ip信息。
	/// </summary>
	[JsonIgnore]
	public ClientIpInfo? ClientIpInfo { get; set; }

	/// <summary>
	/// 应用程序Id字符串。
	/// </summary>
	public string? ApplicationIdentity { get; set; }

	/// <summary>
	/// 服务端用于标记携带对当前应用程序信息对象。
	/// </summary>
	[JsonIgnore]
	public ClientConnectionInfo? ClientConnectionInfo { get; set; }

	/// <summary>
	/// 请求Id字符串。
	/// </summary>
	public string? RequestIdentity { get; set; }

	/// <summary>
	/// 请求方法的名称。
	/// </summary>
	public string? MethodName { get; set; }

	/// <summary>
	/// 请求方法的参数。
	/// </summary>
	public MethodParamsType? MethodParams { get; set; }

	/// <summary>
	/// 请求方法的扩展参数。
	/// </summary>
	[JsonIgnore]
	public List<DataPacketSection>? MethodExtensionParams { get; set; }

	/// <summary>
	/// 当前请求信息是否有效。
	/// </summary>
	public bool IsValid
	{
		get
		{
			if (RequestIdentity?.Length > 0
			    && MethodName?.Length > 0)
			{
				return true;
			}
			return false;
		}
	}


	////////////////////////////////////////////////

	/// <summary>
	/// 数据包。
	/// </summary>
	[JsonIgnore]
	public DataPacket? DataPacket { get; set; }

	/// <summary>
	/// 数据包的字节数组。
	/// </summary>
	[JsonIgnore]
	public ArraySegment<byte>? DataPacketBytes { get; set; }

	////////////////////////////////////////////////

	/// <summary>
	/// Api请求响应的类型。
	/// </summary>
	[JsonIgnore]
	public Type? ApiResponseInfoType { get; set; }

	[JsonIgnore]
	public TaskCompletionSource<ApiResponseInfoAbstract?>? ApiResponseCompletionSource { get; set; }

	[JsonIgnore]
	public string? ApiResponseError { get; set; }

	[JsonIgnore]
	public string? ApiResponseErrorDescription { get; set; }

	[JsonIgnore]
	public Func<ApiResponseInfoAbstract?, Task>? ToReceiveServiceOriginalApiResponseAsync { get; set; }


	////////////////////////////////////////////////
	// 统计相关属性
	////////////////////////////////////////////////

	public ServiceRequestPipeTimeInfo? ServiceRequestPipeTimeInfo { get; set; }

	public ClientRequestPipeTimeInfo? ClientRequestPipeTimeInfo { get; set; }

	#endregion

	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ApiRequestInfo()
	{ }

	public ApiRequestInfo(
	    string applicationIdentity,
	    string requestIdentity,
	    string methodName,
	    MethodParamsType? methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams)
	{
		ApplicationIdentity = applicationIdentity;

		RequestIdentity = requestIdentity;

		MethodName = methodName;

		MethodParams = methodParams;

		if (methodExtensionParams != null)
		{
			MethodExtensionParams ??= [];
			foreach (var methodExtensionParam in methodExtensionParams)
			{
				MethodExtensionParams.Add(methodExtensionParam);
			}
		}
	}

	public void AddExtensionParam(
	    DataPacketSection? extensionParam)
	{
		if (extensionParam == null)
		{
			return;
		}

		if (MethodExtensionParams != null)
		{
			var methodExtensionParams = MethodExtensionParams;
			for (var resultExtensionParamIndex = methodExtensionParams.Count - 1;
			    resultExtensionParamIndex >= 0;
			    resultExtensionParamIndex--)
			{
				var methodExtensionParam = methodExtensionParams[resultExtensionParamIndex];
				if (StringExtension.EqualsIgnoreCase(
				    methodExtensionParam.Name,
				    extensionParam.Name))
				{
					// !!!
					methodExtensionParams.RemoveAt(resultExtensionParamIndex);
					break;
					// !!!
				}
			}
		}
		else
		{
			// !!!
			MethodExtensionParams ??= [];
			// !!!
		}
		MethodExtensionParams.Add(extensionParam);
	}

	public void AddExtensionParam(
	    string? name,
	    string? textValue)
	{
		AddExtensionParam(
		    new DataPacketSection(
		    name,
		    textValue));
	}

	public void AddExtensionParam(
	    string? name,
	    byte[]? bytes)
	{
		AddExtensionParam(
		    new DataPacketSection(
		    name,
		    bytes));
	}

	#endregion
}
