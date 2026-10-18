using BaoXia.Utils.Models;

namespace Boar.Red.Service.BrService.Interfaces;

public interface IServiceApplicationManager
{
	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public Task<IServiceApplicationInfo?> GetServiceApplicationWithIdentityAsync(
	    string? serviceApplicationIdentity,
	    int clientUserId,
	    ClientIpInfo clientIpInfo);

	#endregion
}