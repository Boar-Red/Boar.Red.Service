using BaoXia.Constants;
using BaoXia.Constants.Interfaces;
using BaoXia.Constants.Models;
using BaoXia.Utils.Models;

namespace Boar.Red.Service.ViewModels;

public class ListPageRequestWithClientUserIdAndIpInfo(int clientUserId, ClientIpInfo clientIpInfo)
	: RequestWithClientUserIdAndIpInfo(clientUserId, clientIpInfo), IListPageRequest
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	/// <summary>
	/// 获取列表的起始页的时间点，方便确定分页内容起点。
	/// </summary>
	public DateTime TimeToGetFirstPage { get; set; }

	/// <summary>
	/// 分页索引值，应当大于等于“0”开始。
	/// </summary>
	public int PageIndex { get; set; }

	/// <summary>
	/// 分页元素数，应当大于“0”。
	/// </summary>
	public int PageSize { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @事件节点
	////////////////////////////////////////////////

	#region 事件节点

	protected override Response? DidGetErrorResonseByCheckRequest(
	    RequestPipelineType requestPipelineType)
	{
		var errorResponse = base.DidGetErrorResonseByCheckRequest(requestPipelineType);
		if (errorResponse != null)
		{
			return errorResponse;
		}

		if (this.PageIndex < 0)
		{
			return new()
			{
				Error = Error.InvalidRequest,
				ErrorDescription = "无效的列表分页请求，分页索引值不能小于“0”。"
			};
		}

		if (this.PageSize <= 0)
		{
			return new()
			{
				Error = Error.InvalidRequest,
				ErrorDescription = "无效的列表分页请求，分页元素数不能小于等于“0”。"
			};
		}
		return errorResponse;
	}

	#endregion
}