using BaoXia.Constants.Models;

namespace Boar.Red.Service.BrService.ViewModels.ServiceManager;

public class GetServiceRegisterInfesResponse : Response
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性
	public List<ServiceInfo>? ServiceRegisterInfes { get; set; }

	#endregion
}
