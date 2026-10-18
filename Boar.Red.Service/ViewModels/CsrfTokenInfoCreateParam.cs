using BaoXia.Utils.Models;

namespace Boar.Red.Service.ViewModels;

public class CsrfTokenInfoCreateParam
    (int serviceProviderId, int serviceProviderClientId, int storeId, int userId, ClientIpInfo clientIpInfo,
	double? liveSecondsMaxSpecified, object? additionalParameter)
    : TempTokenCreateParam
    (clientIpInfo, liveSecondsMaxSpecified, additionalParameter)
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public int ServiceProviderId { get; set; } = serviceProviderId;

	public int ServiceProviderClientId { get; set; } = serviceProviderClientId;

	public int StoreId { get; set; } = storeId;

	public int UserId { get; set; } = userId;

	#endregion
}