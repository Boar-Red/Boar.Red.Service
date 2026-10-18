using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils;
using Boar.Red.Service.BrService.Models;

namespace Boar.Red.Service.BrService.Extensions;

public static class ApiResponseInfoExtension
{
	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static ResultClass? ToErrorResult<ResultClass>(this ApiResponseInfo<ResultClass>? apiResponseInfo, string? errorDescriptionPrefix)
	    where ResultClass : Result, new()
	{
		if (errorDescriptionPrefix?.Length > 0)
		{
			errorDescriptionPrefix += "，";
		}

		if (apiResponseInfo == null)
		{
			return new ResultClass()
			{
				Error = Error.ObjectInvalid,
				ErrorDescription = errorDescriptionPrefix + "当前”ApiResponseInfo“对象为”null“。"
			};
		}
		if (apiResponseInfo.IsFailed)
		{
			return new ResultClass()
			{
				Error = Error.OperationFailedByConnection,
				ErrorDescription = $"{errorDescriptionPrefix}连接故障：{apiResponseInfo.StatusCode}，{StringUtil.FirstNotEmpty(apiResponseInfo.StatusDescription?.TrimEnd('。', '.'), "无")}。"
			};
		}
		var apiResult = apiResponseInfo.Result;
		if (apiResult == null)
		{
			return new ResultClass()
			{
				Error = Error.OperationFailedByConnection,
				ErrorDescription = $"{errorDescriptionPrefix}获取到的结果信息为”null“。"
			};
		}
		if (apiResult.IsFailed)
		{
			return new ResultClass()
			{
				Error = apiResult.Error,
				ErrorDescription = $"{errorDescriptionPrefix}错误信息为：{apiResult.Error}，{StringUtil.FirstNotEmpty(apiResult.ErrorDescription?.TrimEnd('。', '.'), "无")}。"
			};
		}
		return null;
	}

	public static ResultType? GetErrorResult<ResultType>(this ApiResponseInfo<ResultType>? apiResponseInfo, string errorDescriptionPrefix)
	    where ResultType : Result, new()
	{
		return ToErrorResult<ResultType>(apiResponseInfo, errorDescriptionPrefix);
	}

	public static Result? ToErrorResult(
	    this ApiResponseInfo<Result>? apiResponseInfo,
	    string errorDescriptionPrefix)
	{
		return ToErrorResult<Result>(apiResponseInfo, errorDescriptionPrefix);
	}

	public static Result? GetErrorResult(
	    this ApiResponseInfo<Result>? apiResponseInfo,
	    string errorDescriptionPrefix)
	{
		return ToErrorResult(
		    apiResponseInfo,
		    errorDescriptionPrefix);
	}

	public static bool IsResultSuccess<ResultType>(this ApiResponseInfo<ResultType>? apiResponseInfo)
	{
		if (apiResponseInfo == null)
		{
			return false;
		}
		if (apiResponseInfo.IsFailed)
		{
			return false;
		}
		var apiResult = apiResponseInfo.Result;
		if (apiResult == null)
		{
			return false;
		}
		if (apiResult is Result bxApiResult
		    && bxApiResult.IsFailed)
		{
			return false;
		}
		return true;
	}

	public static bool IsResultSuccess(this ApiResponseInfo<Result>? apiResponseInfo)
	{
		return IsResultSuccess<Result>(apiResponseInfo);
	}

	#endregion
}