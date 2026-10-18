using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.Models;
using Microsoft.AspNetCore.Mvc;
using static Boar.Red.Service.BrService.Constants.NamingConventions;
using FileInfo = Boar.Red.Service.BrService.Models.FileInfo;

namespace Boar.Red.Service.BrService.Extensions
{
	public static class IActionResultExtension
	{

		////////////////////////////////////////////////
		// @类方法
		////////////////////////////////////////////////

		#region 类方法

		public static ApiResponseInfoAbstract? ToApiResponseInfo(
		    this IActionResult? actionResult,
		    string requestIdentity,
		    out string? actionRedirectUrl)
		{
			//
			actionRedirectUrl = null;
			// 

			ApiResponseInfoAbstract? apiResponseInfo = null;
			if (actionResult is StatusCodeResult statusCodeResult)
			{
				// actionResult = statusCodeResult;
				apiResponseInfo = new ApiResponseInfo<object>(requestIdentity)
				{
					StatusCode = statusCodeResult.StatusCode
				};
			}
			else if (actionResult is ContentResult contentResult)
			{
				var contentType = contentResult.ContentType;
				if (string.IsNullOrEmpty(contentType) != true)
				{
					var indexOfSemicolon = contentType.IndexOf(';');
					if (indexOfSemicolon >= 0)
					{
						contentType = contentType[..indexOfSemicolon];
					}
				}

				apiResponseInfo = new ApiResponseInfo<string>(requestIdentity)
				{
					StatusCode = contentResult.StatusCode,
					MethodResultType = contentType,
					Result = contentResult.Content
				};
			}
			else if (actionResult is FileContentResult fileContentResult)
			{
				// actionResult = fileContentResult;
				apiResponseInfo = new ApiResponseInfo<FileInfo>(requestIdentity)
				{
					StatusCode = (int?)System.Net.HttpStatusCode.OK,
					MethodResultType = System.Net.Mime.MediaTypeNames.Application.Json,
					Result = new FileInfo()
					{
						FileName = fileContentResult.FileDownloadName,
						ContentType = fileContentResult.ContentType,
						LastUpdateTime = fileContentResult.LastModified.ToLocalDateTime()
					}
				};
				{
					apiResponseInfo.AddExtensionParam(
					    DataPacketSectionNames.ApiResponseInfo_FileInfo,
					    fileContentResult.FileContents);
				}
			}
			else if (actionResult is EmptyResult)
			{
				// actionResult = emptyResult;
				apiResponseInfo = new ApiResponseInfo<object>(requestIdentity)
				{
					StatusCode = (int?)System.Net.HttpStatusCode.OK
				};
			}
			else if (actionResult is JsonResult jsonResult)
			{
				var contentType = jsonResult.ContentType;
				if (string.IsNullOrEmpty(contentType) != true)
				{
					var indexOfSemicolon = contentType.IndexOf(';');
					if (indexOfSemicolon >= 0)
					{
						contentType = contentType[..indexOfSemicolon];
					}
				}

				apiResponseInfo = new ApiResponseInfo<object>(requestIdentity)
				{
					StatusCode = jsonResult.StatusCode,
					MethodResultType = contentType,
					Result = jsonResult.Value
				};
			}
			else if (actionResult is RedirectResult redirectResult)
			{
				var redirectUrl = redirectResult.Url;
				// 绝对地址：
				if (string.IsNullOrEmpty(redirectUrl)
				    || redirectUrl.Contains(
				    System.Uri.SchemeDelimiter,
				    StringComparison.OrdinalIgnoreCase) == true)
				{
					// actionResult = redirectResult;
					apiResponseInfo = new ApiResponseInfo<string>(requestIdentity)
					{
						StatusCode = (int?)System.Net.HttpStatusCode.Redirect,
						RedirectUrl = redirectUrl
					};
				}
				else
				{
					// !!!
					actionRedirectUrl = redirectUrl;
					// !!!
				}
			}
			else
			{
				apiResponseInfo = new ApiResponseInfo<object>(requestIdentity)
				{
					StatusCode = (int)System.Net.HttpStatusCode.UnsupportedMediaType
				};
			}

			////////////////////////////////////////////////
			// 统一检查和完善Http状态码：
			////////////////////////////////////////////////
			if (apiResponseInfo != null)
			{
				if (apiResponseInfo.StatusCode == null
				    || apiResponseInfo.StatusCode == 0)
				{
					apiResponseInfo.StatusCode = (int)System.Net.HttpStatusCode.OK;
				}
			}
			return apiResponseInfo;
		}

		#endregion

	}
}
