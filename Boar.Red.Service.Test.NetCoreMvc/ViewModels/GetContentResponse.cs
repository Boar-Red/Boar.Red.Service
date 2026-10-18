using BaoXia.Constants.Models;

namespace Boar.Red.Service.Test.NetCoreMvc.ViewModels;

public class GetContentResponse<ContentType> : Response
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ContentType? Content { get; set; }

	#endregion
}