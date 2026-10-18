using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils.Models;

namespace Boar.Red.Service.ViewModels;

public class RequestWithUserToken(string userToken, ClientIpInfo clientInfo)
	: RequestWithClientIpInfo(clientInfo)
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string UserToken { get; set; } = userToken;


	#endregion


	////////////////////////////////////////////////
	// @重载
	////////////////////////////////////////////////

	#region 重载

	protected override Response? DidGetErrorResonseByCheckRequest(RequestPipelineType requestPipelineType)
	{
		var errorResponse = base.DidGetErrorResonseByCheckRequest(requestPipelineType);
		if (errorResponse?.IsFailed == true)
		{
			return errorResponse;
		}

		if (string.IsNullOrWhiteSpace(UserToken))
		{
			return new()
			{
				Error = Error.UserTokenNotExisted,
				ErrorDescription = "请求无效，用户令牌为“空”。"
			};
		}

		return null;
	}


	#endregion
}
