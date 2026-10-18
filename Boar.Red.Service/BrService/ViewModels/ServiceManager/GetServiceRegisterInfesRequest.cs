using BaoXia.Constants.Models;

namespace Boar.Red.Service.BrService.ViewModels.ServiceManager;

public class GetServiceRegisterInfesRequest : Request
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string? GroupName { get; set; }

	public string? ServiceName { get; set; }

	public string? NodeName { get; set; }

	public string? ServiceVersionMin { get; set; }

	public string? ServiceVersionMax { get; set; }

	#endregion
}