using BaoXia.Utils.Models;

namespace Boar.Red.Service.BrService.ViewModels;

public class ClientUserIdAndIpInfo(int clientUserId, ClientIpInfo clientIpInfo)
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public int ClientUserId { get; set; } = clientUserId;

	public ClientIpInfo ClientIpInfo { get; set; } = clientIpInfo;

	#endregion
}