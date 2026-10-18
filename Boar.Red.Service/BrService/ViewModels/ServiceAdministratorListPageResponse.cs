using BaoXia.Constants.Models;

namespace Boar.Red.Service.BrService.ViewModels;

public class ServiceAdministratorListPageResponse
    <ListElementType>
    : ListPageResponse<ListElementType>
{
	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ServiceAdministratorListPageResponse()
	{ }

	public ServiceAdministratorListPageResponse(
	    ServiceAdministratorListPageRequest? listPageRequest)
	{
		if (listPageRequest == null)
		{
			return;
		}

		PageIndex = listPageRequest.PageIndex;
		PageSize = listPageRequest.PageSize;
	}

	#endregion
}