using Boar.Red.Service.Constants;
using Boar.Red.Service.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Boar.Red.Service.Extensions;

public static class IHeaderDictionaryExtension
{
	////////////////////////////////////////////////
	// @类方法：从【HttpHeader】中获取客户端信息。
	////////////////////////////////////////////////

	#region 类方法

	public static BrApiClientInfo GetClientInfo(this IHeaderDictionary httpHeaders,
		IReadOnlyDictionary<string, string?>? customHttpHeaders = null)
	{
		return new BrApiClientInfo(
		    TryGetClientInfoValue(httpHeaders, customHttpHeaders, HttpHeaderKeys.BrApiClientInfo.ServiceProviderClientIdentity),
		    TryGetClientInfoValue(httpHeaders, customHttpHeaders, HttpHeaderKeys.BrApiClientInfo.Version),
		    TryGetClientInfoValue(httpHeaders, customHttpHeaders, HttpHeaderKeys.BrApiClientInfo.InstallChannel),
		    TryGetClientInfoValue(httpHeaders, customHttpHeaders, HttpHeaderKeys.BrApiClientInfo.OSName),
		    TryGetClientInfoValue(httpHeaders, customHttpHeaders, HttpHeaderKeys.BrApiClientInfo.OSVersion),
		    TryGetClientInfoValue(httpHeaders, customHttpHeaders, HttpHeaderKeys.BrApiClientInfo.DeviceName),
		    TryGetClientInfoValue(httpHeaders, customHttpHeaders, HttpHeaderKeys.BrApiClientInfo.DeviceId),
		    null,
		    null);
	}

	/// <summary>
	/// 获取指定“客户端信息”请求头的值：
	/// 优先取“自定义 Http 请求头”中的值，为空时，回退到“Http 请求头”中的值。
	/// </summary>
	/// <param name="httpHeaders">当前 Http 请求头。</param>
	/// <param name="customHttpHeaders">当前连接级的自定义 Http 请求头，可为“null”（如 HTTP 请求场景）。</param>
	/// <param name="httpHeaderKey">目标请求头键名。</param>
	private static string? TryGetClientInfoValue(
	    IHeaderDictionary httpHeaders,
	    IReadOnlyDictionary<string, string?>? customHttpHeaders,
	    string httpHeaderKey)
	{
		string? headerValue = null;
		customHttpHeaders?.TryGetValue(httpHeaderKey, out headerValue);
		if (string.IsNullOrEmpty(headerValue)
			&& httpHeaders.TryGetValue(httpHeaderKey, out var headerValueInHttpHeader))
		{
			headerValue = headerValueInHttpHeader;
		}
		return headerValue;
	}

	#endregion
}