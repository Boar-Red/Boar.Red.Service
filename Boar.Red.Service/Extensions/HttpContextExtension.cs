using Boar.Red.Service.Constants;
using Boar.Red.Service.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Boar.Red.Service.Extensions;

public static class HttpContextExtension
{
	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static TokenCheckResultInfo? GetTokenCheckResultInfo(this HttpContext httpContext)
	{
		if (httpContext.Items.TryGetValue(
			HttpContextItemKeys.BrServiceTokenManager.TokenCheckResultInfo, out var tokenCheckResultInfoObject)
			&& tokenCheckResultInfoObject is TokenCheckResultInfo tokenCheckResultInfo)
		{
			return tokenCheckResultInfo;
		}
		return null;
	}

	#endregion
}