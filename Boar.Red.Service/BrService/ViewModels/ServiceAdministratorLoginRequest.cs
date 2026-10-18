using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils.Models;
using Boar.Red.Service.ViewModels;

namespace Boar.Red.Service.BrService.ViewModels;

public class ServiceAdministratorLoginRequest(string account, string password, int clientUserId, ClientIpInfo clientIpInfo)
    : RequestWithClientIpInfo(clientIpInfo)
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string Account { get; set; } = account;

	public string Password { get; set; } = password;

	public int ClientUserId { get; set; } = clientUserId;

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

		if (string.IsNullOrWhiteSpace(Account))
		{
			return new()
			{
				Error = Error.UserAccountInvalid,
				ErrorDescription = "登录请求无效，用户账号不能为空。"
			};
		}

		if (string.IsNullOrWhiteSpace(Password))
		{
			return new()
			{
				Error = Error.UserAccountInvalid,
				ErrorDescription = "登录请求无效，用户密码不能为空。"
			};
		}

		return null;
	}

	#endregion
}