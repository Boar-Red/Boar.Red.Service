using BaoXia.Constants.Models;

namespace Boar.Red.Service.BrService.ViewModels;

public class ServiceAdministratorLoginResponse : Response
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string? SessionToken { get; set; }

	#endregion
}