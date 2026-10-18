using BaoXia.Constants.Models;

namespace Boar.Red.Service.Test.NetCoreMvc.ViewModels;

public class SearchContentResponse<ContentType> : Response
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public int ContentCountSearched { get; set; }

	public int PageIndex { get; set; }

	public int PageSize { get; set; }

	public IEnumerable<ContentType>? ContentInPage { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public SearchContentResponse()
	{ }

	public SearchContentResponse(SearchContentRequest? request)
	{
		if (request == null)
		{
			return;
		}

		PageIndex = request.PageIndex;
		PageSize = request.PageSize;
	}

	#endregion

}