using BaoXia.Constants.Models;

namespace Boar.Red.Service.Test.NetCoreMvc.ViewModels;

public class SaveContentResult<ContentType> : Result
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ContentType? Content { get; set; }

	#endregion
}