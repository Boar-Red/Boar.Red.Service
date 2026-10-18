using BaoXia.Constants;
using BaoXia.Constants.Models;

namespace Boar.Red.Service.Test.NetCoreMvc.ViewModels;

public class GetContentWithIdRequest : Request
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public int ContentId { get; set; }


	protected override BaoXia.Constants.Models.Response? DidGetErrorResonseByCheckRequest(RequestPipelineType requestPipelineType)
	{
		var errorResponse = base.DidGetErrorResonseByCheckRequest(requestPipelineType);
		if (errorResponse?.IsFailed == true)
		{
			return errorResponse;
		}
		if (ContentId == 0)
		{
			return new()
			{
				Error = Error.InvalidRequest,
				ErrorDescription = "请求无效，“ContentId”不能为“0”。"
			};
		}
		return null;
	}

	#endregion
}