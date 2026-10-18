using BaoXia.Constants.Models;

namespace Boar.Red.Service.Test.NetCoreMvc.ViewModels;

public class SaveContentResponse<ContentType> : Response
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ContentType? Content { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public void SetResult(SaveContentResult<ContentType>? result)
	{
		if (result == null)
		{
			return;
		}

		Error = result.Error;
		ErrorDescription = result.ErrorDescription;

		Content = result.Content;
	}

	#endregion
}