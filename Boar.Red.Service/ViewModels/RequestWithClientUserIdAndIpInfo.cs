using BaoXia.Utils.Models;

namespace Boar.Red.Service.ViewModels;

public class RequestWithClientUserIdAndIpInfo(int clientUserId, ClientIpInfo clientIpInfo)
    : RequestWithClientIpInfo(clientIpInfo)
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public int ClientUserId { get; set; } = clientUserId;

	#endregion
}