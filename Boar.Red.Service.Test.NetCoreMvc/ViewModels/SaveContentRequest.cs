using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils.Extensions;

namespace Boar.Red.Service.Test.NetCoreMvc.ViewModels;

public class SaveContentRequest<ContentType> : Request
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ContentType? Content { get; set; }

	protected override BaoXia.Constants.Models.Response? DidGetErrorResonseByCheckRequest(RequestPipelineType requestPipelineType)
	{
		var errorResponse = base.DidGetErrorResonseByCheckRequest(requestPipelineType);
		if (errorResponse?.IsFailed == true)
		{
			return errorResponse;
		}
		if (Content != null)
		{
			var isContentValidResult
				= ObjectExtension.GetPropertyValueWithName(
					Content,
					"IsValid");
			if (isContentValidResult == null)
			{
				return null;
			}
			if (isContentValidResult is bool isContentValid
				&& isContentValid == false)
			{
				return new()
				{
					Error = BaoXia.Constants.Error.InvalidRequest,
					ErrorDescription = $"请求无效，“{nameof(Content)}”无效。"
				};
			}
		}
		return null;
	}

	#endregion
}