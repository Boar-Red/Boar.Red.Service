using Boar.Red.Service.BrService.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Boar.Red.Service.BrService.ViewModels;

public class ServiceActionResultInfo(IActionResult? result, BrServiceResponseOptionAttribute? responseOption)
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public IActionResult? Result { get; set; } = result;

	public BrServiceResponseOptionAttribute? ResponseOption { get; set; } = responseOption;

	#endregion
}