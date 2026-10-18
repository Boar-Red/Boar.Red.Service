using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils.Models;

namespace Boar.Red.Service.BrService.ViewModels;

public class ServiceAdministratorListPageRequest(int clientUserId, ClientIpInfo clientIpInfo)
    : ServiceAdministratorRequest(clientUserId, clientIpInfo)
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public DateTime TimeToGetFirstPage { get; set; }

	public int PageIndex { get; set; }

	public int PageSize { get; set; }

	public string? SearchKey { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @重载
	////////////////////////////////////////////////

	#region 重载

	protected override Response? DidGetErrorResonseByCheckRequest(RequestPipelineType requestPipelineType)
	{
		var baseErrorResponse = base.DidGetErrorResonseByCheckRequest(requestPipelineType);
		if (baseErrorResponse != null)
		{
			return baseErrorResponse;
		}

		if (PageIndex < 0)
		{
			return new Response
			{
				Error = "BaoXia.Constants.Error.InvalidRequest",
				ErrorDescription = "无效的列表分页请求，分页索引值不能小于“0”。"
			};
		}

		if (PageSize <= 0)
		{
			return new Response
			{
				Error = "BaoXia.Constants.Error.InvalidRequest",
				ErrorDescription = "无效的列表分页请求，分页元素数不能小于等于“0”。"
			};
		}
		return null;
	}

	#endregion
}