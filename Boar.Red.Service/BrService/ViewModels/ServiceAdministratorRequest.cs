using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils.Models;
using Boar.Red.Service.ViewModels;

namespace Boar.Red.Service.BrService.ViewModels;

public class ServiceAdministratorRequest(int clientUserId, ClientIpInfo clientIpInfo)
	: RequestWithClientIpInfo(clientIpInfo)
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string? ServiceAdministratorSessionToken { get; set; }

	public int ClientUserId { get; set; } = clientUserId;


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

		if (requestPipelineType == RequestPipelineType.BxService)
		{
			if (string.IsNullOrEmpty(ServiceAdministratorSessionToken))
			{
				return new Response()
				{
					Error = Error.UserTokenNotExisted,
					ErrorDescription = "管理员会话令牌为“空”。"
				};
			}
		}
		return null;
	}

	#endregion
}