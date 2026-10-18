using BaoXia.Utils.Models;

namespace Boar.Red.Service.ViewModels;

public class CsrfTokenInfo : TempTokenInfo
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public int ServiceProviderId { get; set; }

	public int ServiceProviderClientId { get; set; }

	public int StoreId { get; set; }

	public int UserId { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public CsrfTokenInfo()
	{
	}

	public CsrfTokenInfo(
	    int serviceProviderId,
	    int serviceProviderClientId,
	    int storeId,
	    int userId,
	    string tokenValue,
	    double? liveSecondsMaxSpecified,
	    ClientIpInfo clientIpInfo,
	    DateTimeOffset createTime)
	    : base(
	    tokenValue,
	    clientIpInfo,
	    liveSecondsMaxSpecified,
	    createTime)
	{
		this.ServiceProviderId = serviceProviderId;
		this.ServiceProviderClientId = serviceProviderClientId;
		this.StoreId = storeId;
		this.UserId = userId;
	}

	#endregion
}