using BaoXia.Constants.Models;
using Boar.Red.Service.Constants;
using System.Net;

namespace Boar.Red.Service.ViewModels;

public class TokenCheckResultInfo
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public TokenCheckResult CheckResult { get; set; }

	public HttpStatusCode ResponseStatusCode { get; set; }

	public Response? ResponseResult { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public TokenCheckResultInfo()
	{ }

	public TokenCheckResultInfo(
	    TokenCheckResult checkResult,
	    HttpStatusCode responseStatusCode,
	    Response? responseResult)
	{
		CheckResult = checkResult;
		ResponseStatusCode = responseStatusCode;
		ResponseResult = responseResult;
	}

	#endregion
}