using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.Models.StatisticsInfo;
using System.Net.WebSockets;
using System.Text.Json.Serialization;

namespace Boar.Red.Service.BrService.Models;

public abstract class ApiResponseInfoAbstract
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
	/// 请求Id字符串。
	/// </summary>
	public string? RequestIdentity { get; set; }

	/// <summary>
	/// 数据加密使用的AesKey，如果为“空”，则不加密数据。
	/// </summary>
	public string? AesKey { get; set; }

	/// <summary>
	/// 调用结果代码，同HTTP代码：System.Net.HttpStatusCode。
	/// </summary>
	public int? StatusCode { get; set; }

	/// <summary>
	/// 本次调用是否成功，
	/// 注意里的成功是指调用是否成功，而不是业务逻辑是否成功。
	/// </summary>
	public bool IsSuccess
	{
		get
		{
			if (StatusCode == 200)
			{
				return true;
			}
			return false;
		}
	}

	/// <summary>
	/// 本次调用是否失败，
	/// 注意里的成功是指调用是否成功，而不是业务逻辑是否成功。
	/// </summary>
	public bool IsFailed => !IsSuccess;

	/// <summary>
	/// 调用结果状态的描述信息。
	/// </summary>
	public string? StatusDescription { get; set; }

	/// <summary>
	/// 重定向的目标Url。
	/// </summary>
	public string? RedirectUrl { get; set; }

	/// <summary>
	/// 方法调用结果参数类型。
	/// </summary>
	public string? MethodResultType { get; set; }

	/// <summary>
	/// 方法调用结果参数类型。
	/// </summary>
	[JsonIgnore]
	public abstract Type ResultTypeInfo { get; }

	/// <summary>
	/// 方法调用结果参数。
	/// </summary>
	[JsonIgnore]
	public abstract object? ResultInObject { get; set; }

	/// <summary>
	/// 响应方法的扩展参数。
	/// </summary>
	[JsonIgnore]
	public List<DataPacketSection>? ResultAttachmentBytes { get; set; }

	/// <summary>
	/// 当前响应信息是否有效。
	/// </summary>
	public bool IsValid
	{
		get
		{
			if (RequestIdentity?.Length > 0)
			{
				return true;
			}
			return false;
		}
	}

	/// <summary>
	/// 当前任务的完成源。
	/// </summary>
	[JsonIgnore]
	public TaskCompletionSource<bool>? ApiResponseSentCompletionSource { get; set; }

	////////////////////////////////////////////////
	// 数据包相关属性
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
	public byte[]? DataPacketBytes { get; set; }

	////////////////////////////////////////////////
	// 统计相关属性
	////////////////////////////////////////////////

	public ServiceRequestPipeTimeInfo? ServiceRequestPipeTimeInfo { get; set; }

	#endregion

	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ApiResponseInfoAbstract()
	{ }

	public ApiResponseInfoAbstract(string requestIdentity)
	{
		RequestIdentity = requestIdentity;
	}

	public void AddExtensionParam(
	    DataPacketSection? extensionParam)
	{
		if (extensionParam != null)
		{
			if (ResultAttachmentBytes != null)
			{
				var resultExtensionParams = ResultAttachmentBytes;
				for (var resultExtensionParamIndex = resultExtensionParams.Count - 1;
				    resultExtensionParamIndex >= 0;
				    resultExtensionParamIndex--)
				{
					var resultExtensionParam = resultExtensionParams[resultExtensionParamIndex];
					if (StringExtension.EqualsIgnoreCase(
					    resultExtensionParam.Name,
					    extensionParam.Name))
					{
						// !!!
						resultExtensionParams.RemoveAt(resultExtensionParamIndex);
						break;
						// !!!
					}
				}
			}
			else
			{
				// !!!
				ResultAttachmentBytes ??= [];
				// !!!
			}
			ResultAttachmentBytes.Add(extensionParam);
		}
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
