using BaoXia.Constants;
using BaoXia.Constants.Models;

namespace Boar.Red.Service.Test.NetCoreMvc.ViewModels;

public class SearchContentRequest : Request
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string? SearchKey { get; set; }

	public int PageIndex { get; set; }

	public int PageSize { get; set; }

	protected override BaoXia.Constants.Models.Response? DidGetErrorResonseByCheckRequest(RequestPipelineType requestPipelineType)
	{
		var errorResponse = base.DidGetErrorResonseByCheckRequest(requestPipelineType);
		if (errorResponse?.IsFailed == true)
		{
			return errorResponse;
		}
		if (PageIndex < 0)
		{
			return new()
			{
				Error = Error.InvalidRequest,
				ErrorDescription = $"请求无效，“{nameof(PageIndex)}”不能小于“0”。"
			};
		}
		if (PageSize <= 0)
		{
			return new()
			{
				Error = Error.InvalidRequest,
				ErrorDescription = $"请求无效，“{nameof(PageSize)}”不能小于等于“0”。"
			};
		}
		return null;
	}

	#endregion
}
