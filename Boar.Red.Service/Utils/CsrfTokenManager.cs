using BaoXia.Utils;
using BaoXia.Utils.Models;
using Boar.Red.Service.ViewModels;

namespace Boar.Red.Service.Utils;

public class CsrfTokenManager
    (Func<double> toGetTokenLiveSecondsMax,
    Func<int>? toGetTokenValueLength,
    Func<double>? toGetTokenCleanIntervalSeconds = null)
    : TempTokenManager
    <CsrfTokenInfo, CsrfTokenInfoCreateParam>
    (CsrfTokenManagerName,
    toGetTokenLiveSecondsMax,
    toGetTokenValueLength,
    toGetTokenCleanIntervalSeconds)
{

	////////////////////////////////////////////////
	// @静态常量
	////////////////////////////////////////////////

	#region 静态常量

	public const string CsrfTokenManagerName = "Boar.Red 服务，CSRF令牌管理器";

	#endregion


	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法
	public static bool IsTokenInfoValid(
	    CsrfTokenInfo? tokenInfo,
	    int serviceProviderId,
	    int serviceProviderClientId,
	    int storeId,
	    int userId)
	{
		if (tokenInfo == null)
		{
			return false;
		}

		if (tokenInfo.ServiceProviderId == serviceProviderId
		&& tokenInfo.ServiceProviderClientId == serviceProviderClientId
		&& tokenInfo.StoreId == storeId
		&& (tokenInfo.UserId == 0
		    || tokenInfo.UserId == userId))
		{
			// !!!
			return true;
			// !!!
		}
		return false;
	}

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	protected new async Task<CsrfTokenInfo> CreateTokenInfoAsync(
	    CsrfTokenInfoCreateParam tokenCreateParam,
	    DateTimeOffset createTime)
	{
		return await base.CreateTokenInfoAsync(tokenCreateParam, createTime);
	}

	public async Task<CsrfTokenInfo> CreateTokenInfoAsync(
	    int serviceProviderId,
	    int serviceProviderClientId,
	    int storeId,
	    int userId,
	    ClientIpInfo clientIpInfo,
	    double? liveSecondsMaxSpecified,
	    object? additionalParameter,
	    DateTimeOffset createTime)
	{
		return await base.CreateTokenInfoAsync(
		    new(serviceProviderId,
		    serviceProviderClientId,
		    storeId,
		    userId,
		    clientIpInfo,
		    liveSecondsMaxSpecified,
		    additionalParameter),
		    createTime);
	}

	protected new async Task<string> CreateTokenAsync(
	    CsrfTokenInfoCreateParam tokenCreateParam,
	    DateTimeOffset createTime)
	{
		return await base.CreateTokenAsync(tokenCreateParam, createTime);
	}

	public async Task<string> CreateTokenAsync(
	    int serviceProviderId,
	    int serviceProviderClientId,
	    int storeId,
	    int userId,
	    ClientIpInfo clientIpInfo,
	    double? liveSecondsMaxSpecified,
	    object? additionalParameter,
	    DateTimeOffset createTime)
	{
		return await base.CreateTokenAsync(
		    new(serviceProviderId,
		    serviceProviderClientId,
		    storeId,
		    userId,
		    clientIpInfo,
		    liveSecondsMaxSpecified,
		    additionalParameter),
		    createTime);
	}

	protected new async Task<bool> IsTokenValidAsync(
	    string? tokenValue,
	    ClientIpInfo clientIpInfo,
	    DateTimeOffset? checkTime = null)
	{
		return await base.IsTokenValidAsync(tokenValue, clientIpInfo, checkTime);
	}

	public async Task<bool> IsTokenValidAsync(
	    string? tokenValue,
	    int serviceProviderId,
	    int serviceProviderClientId,
	    int storeId,
	    int userId,
	    ClientIpInfo clientIpInfo,
	    DateTimeOffset? checkTime = null)
	{
		var validTokenInfo
		    = await GetValidTokenInfoAsync(
		    tokenValue,
		    clientIpInfo,
		    checkTime);
		return IsTokenInfoValid(
		    validTokenInfo,
		    serviceProviderId,
		    serviceProviderClientId,
		    storeId,
		    userId);
	}

	#endregion


	////////////////////////////////////////////////
	// @重载
	////////////////////////////////////////////////

	#region 重载

	protected override async Task<CsrfTokenInfo> DidCreateTempTokenInfoAsync(
	    CsrfTokenInfoCreateParam tokenCreateParam,
	    DateTimeOffset createTime)
	{
		var tokenInfo = await base.DidCreateTempTokenInfoAsync(
		    tokenCreateParam,
		    createTime);
		{
			tokenInfo.ServiceProviderId = tokenCreateParam.ServiceProviderId;
			tokenInfo.ServiceProviderClientId = tokenCreateParam.ServiceProviderClientId;
			tokenInfo.StoreId = tokenCreateParam.StoreId;
			tokenInfo.UserId = tokenCreateParam.UserId;
		}
		return tokenInfo;
	}

	#endregion
}