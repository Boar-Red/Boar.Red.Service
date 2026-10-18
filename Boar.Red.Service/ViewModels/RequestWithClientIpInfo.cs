using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils.Models;

namespace Boar.Red.Service.ViewModels;

public class RequestWithClientIpInfo(ClientIpInfo clientIpInfo) : Request
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ClientIpInfo ClientIpInfo { get; set; } = clientIpInfo;

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

		if (requestPipelineType == RequestPipelineType.Http)
		{
			return null;
		}

		if (ClientIpInfo == null)
		{
			return new()
			{
				Error = Error.InvalidRequest,
				ErrorDescription = "请求无效，客户端Ip信息为“null”。"
			};
		}
		return null;
	}

	#endregion
}