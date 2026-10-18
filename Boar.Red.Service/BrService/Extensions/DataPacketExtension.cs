using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.Constants;
using Boar.Red.Service.BrService.Models;
using static Boar.Red.Service.BrService.Constants.NamingConventions;

namespace Boar.Red.Service.BrService.Extensions;

public static class DataPacketExtension
{
	////////////////////////////////////////////////
	// @静态方法
	////////////////////////////////////////////////

	#region 静态方法

	public static DataPacket ToDataPacket(this ApiRequestInfo<object> apiRequestInfo, System.Text.Encoding? textEncoding,
	    DataPackageEncryptionMethod encryptionMethod, string encryptionAesKey)
	{
		DataPacket dataPacket = new(null, textEncoding, encryptionMethod, encryptionAesKey);
		{
			////////////////////////////////////////////////
			// 1/4，请求信息公用字段：
			////////////////////////////////////////////////
			{
				//dataPacket.SetHeaderParam(
				//        DataPacketHeaderName.MethodName,
				//        apiRequestInfo.MethodName);
			}

			////////////////////////////////////////////////
			// 2/4，请求信息基础字段：
			////////////////////////////////////////////////
			{
				dataPacket.AddDataSection(DataPacketSectionNames.ApiRequestInfo, apiRequestInfo);
			}

			////////////////////////////////////////////////
			// 3/4，方法调用参数（基本）：
			////////////////////////////////////////////////
			//if (apiRequestInfo.MethodParamsInObject != null)
			//{
			//        dataPacket.AddDataSection(
			//                DataPacketSectionNames.ApiRequestInfo_MethodParams,
			//                apiRequestInfo.MethodParamsInObject);
			//}

			////////////////////////////////////////////////
			// 4/4，方法调用参数（扩展）：
			////////////////////////////////////////////////
			{
				dataPacket.AddDataSectionRange(apiRequestInfo.MethodExtensionParams);
			}
		}
		return dataPacket;
	}

	public static bool PrepareDataPacketProperties(
	    this ApiRequestInfo<object> apiRequestInfo,
	    System.Text.Encoding textEncoding,
	    DataPackageEncryptionMethod encryptionMethod,
	    string encryptionAesKey,
	    int dataPacketSizeToCompression,
	    string? dataPacketCompressionMethodName)
	{
		var dataPacket = apiRequestInfo.ToDataPacket(textEncoding, encryptionMethod, encryptionAesKey);
		var dataPacketBytes = dataPacket.ToBytes(textEncoding, encryptionMethod, encryptionAesKey, dataPacketSizeToCompression,
			dataPacketCompressionMethodName);
		if (dataPacketBytes.Length < 1)
		{
			return false;
		}

		// !!!
		apiRequestInfo.DataPacket = dataPacket;
		apiRequestInfo.DataPacketBytes = dataPacketBytes;
		// !!!

		return true;
	}

	public static bool TryToPrepareDataPacketProperties(
	    this ApiRequestInfo<object> apiRequestInfo,
	    System.Text.Encoding textEncoding,
	    DataPackageEncryptionMethod encryptionMethod,
	    string encryptionAesKey,
	    int dataPacketSizeToCompression,
	    string? dataPacketCompressionMethodName)
	{
		if (apiRequestInfo.DataPacket != null
		    && apiRequestInfo.DataPacketBytes?.Count > 0)
		{
			return true;
		}
		return PrepareDataPacketProperties(
		    apiRequestInfo,
		    textEncoding,
		    encryptionMethod,
		    encryptionAesKey,
		    dataPacketSizeToCompression,
		    dataPacketCompressionMethodName);
	}
	public static ApiRequestInfo<MethodParamsType>? ToApiRequestInfo<MethodParamsType>(
	    this DataPacket dataPacket)
	{
		var dataPacketTextEncoding
		    = dataPacket.TextEncoding;

		////////////////////////////////////////////////
		// 1/3，请求信息基础字段：
		////////////////////////////////////////////////
		var apiRequestInfoDataSection
		    = dataPacket.GetDataPacketSectionWithSectionName(
		    DataPacketSectionNames.ApiRequestInfo);
		var apiRequestInfo
		    = apiRequestInfoDataSection?.GetObjectValue<ApiRequestInfo<MethodParamsType>>(
		    dataPacketTextEncoding);
		if (apiRequestInfo == null)
		{
			return null;
		}

		////////////////////////////////////////////////
		// 2/3，方法调用参数（基本）：
		////////////////////////////////////////////////
		var apiRequestMethodParamsDataSection
		    = dataPacket.GetDataPacketSectionWithSectionName(
		    DataPacketSectionNames.ApiRequestInfo_MethodParams);
		if (apiRequestMethodParamsDataSection != null)
		{
			apiRequestInfo.MethodParams
			    = apiRequestMethodParamsDataSection.GetObjectValue<MethodParamsType>(
			    dataPacketTextEncoding);
		}

		////////////////////////////////////////////////
		// 3/3，方法调用参数（扩展）：
		////////////////////////////////////////////////
		foreach (var dataPacketSection in dataPacket)
		{
			if (DataPacketSectionNames.ApiRequestInfo.EqualsIgnoreCase(dataPacketSection.Name)
			    == false
			    && DataPacketSectionNames.ApiRequestInfo_MethodParams.EqualsIgnoreCase(dataPacketSection.Name)
			    == false)
			{
				apiRequestInfo.AddExtensionParam(dataPacketSection);
			}
		}
		return apiRequestInfo;
	}


	////////////////////////////////////////////////
	////////////////////////////////////////////////
	////////////////////////////////////////////////


	public static DataPacket ToDataPacket(
	    this ApiResponseInfoAbstract apiResponseInfo,
	    System.Text.Encoding? dataPacketTextEncoding,
	    DataPackageEncryptionMethod encryptionMethod,
	    string encryptionAesKey)
	{
		DataPacket dataPacket = new(
		    null,
		    dataPacketTextEncoding,
		    encryptionMethod,
		    encryptionAesKey);
		{
			////////////////////////////////////////////////
			// 1/4，响应信息基础字段：
			////////////////////////////////////////////////
			{
				dataPacket.SetHeaderParam(
				    DataPacketHeaderName.RequestIdentity,
				    apiResponseInfo.RequestIdentity);
			}

			////////////////////////////////////////////////
			// 2/4，响应信息基础字段：
			////////////////////////////////////////////////
			{
				dataPacket.AddDataSection(
				    DataPacketSectionNames.ApiResponseInfo,
				    apiResponseInfo);
			}

			////////////////////////////////////////////////
			// 3/4，响应调用参数（基本）：
			////////////////////////////////////////////////
			if (apiResponseInfo.ResultInObject != null)
			{
				dataPacket.AddDataSection(
				    DataPacketSectionNames.ApiResponseInfo_MethodParams,
				    apiResponseInfo.ResultInObject);
			}

			////////////////////////////////////////////////
			// 4/4，响应调用参数（扩展）：
			////////////////////////////////////////////////
			{
				dataPacket.AddDataSectionRange(
				    apiResponseInfo.ResultAttachmentBytes);
			}
		}
		return dataPacket;
	}

	public static bool PrepareDataPacketProperties(
	    this ApiResponseInfoAbstract apiResponseInfo,
	    System.Text.Encoding textEncoding,
	    DataPackageEncryptionMethod encryptionMethod,
	    string encryptionAesKey,
	    int dataPacketSizeToCompression,
	    string? dataPacketCompressionMethodName)
	{
		var dataPacket
		    = apiResponseInfo
		    .ToDataPacket(
		    null,
		    encryptionMethod,
		    encryptionAesKey);
		var dataPacketBytes
		    = dataPacket
		    .ToBytes(
		    textEncoding,
		    encryptionMethod,
		    encryptionAesKey,
		    dataPacketSizeToCompression,
		    dataPacketCompressionMethodName);
		if (dataPacketBytes.Length < 1)
		{
			return false;
		}

		// !!!
		apiResponseInfo.DataPacket = dataPacket;
		apiResponseInfo.DataPacketBytes = dataPacketBytes;
		// !!!

		return true;
	}

	public static bool TryToPrepareDataPacketProperties(
	    this ApiResponseInfoAbstract apiResponseInfo,
	    System.Text.Encoding textEncoding,
	    DataPackageEncryptionMethod encryptionMethod,
	    string encryptionAesKey,
	    int dataPacketSizeToCompression,
	    string? dataPacketCompressionMethodName)
	{
		if (apiResponseInfo.DataPacket != null
		    && apiResponseInfo.DataPacketBytes?.Length > 0)
		{
			return true;
		}
		return PrepareDataPacketProperties(
		    apiResponseInfo,
		    textEncoding,
		    encryptionMethod,
		    encryptionAesKey,
		    dataPacketSizeToCompression,
		    dataPacketCompressionMethodName);
	}

	public static ApiResponseInfoAbstract? ToApiResponseInfo(this DataPacket dataPacket, Type apiResponseInfoType)
	{
		var dataPacketTextEncoding = dataPacket.TextEncoding;

		////////////////////////////////////////////////
		// 1/3，响应信息基础字段：
		////////////////////////////////////////////////
		var apiResponseInfoDataSection = dataPacket.GetDataPacketSectionWithSectionName(DataPacketSectionNames.ApiResponseInfo);
		if (apiResponseInfoDataSection?.GetObjectValue(apiResponseInfoType, dataPacketTextEncoding)
			is not ApiResponseInfoAbstract apiResponseInfo)
		{
			return null;
		}

		////////////////////////////////////////////////
		// 2/3，方法响应结果参数（基本）：
		////////////////////////////////////////////////
		var apiResponseMethodParamsDataSection = dataPacket.GetDataPacketSectionWithSectionName(
		    DataPacketSectionNames.ApiResponseInfo_MethodParams);
		if (apiResponseMethodParamsDataSection != null)
		{
			apiResponseInfo.ResultInObject = apiResponseMethodParamsDataSection.GetObjectValue(
			    apiResponseInfo.ResultTypeInfo, dataPacketTextEncoding);
		}

		////////////////////////////////////////////////
		// 3/3，方法响应结果参数（扩展）：
		////////////////////////////////////////////////
		foreach (var dataPacketSection in dataPacket)
		{
			if (DataPacketSectionNames.ApiResponseInfo.EqualsIgnoreCase(dataPacketSection.Name) == false
			    && DataPacketSectionNames.ApiResponseInfo_MethodParams.EqualsIgnoreCase(dataPacketSection.Name) == false)
			{
				apiResponseInfo.AddExtensionParam(dataPacketSection);
			}
		}
		return apiResponseInfo;
	}

	public static ApiResponseInfo<MethodResultClass>? ToApiResponseInfo<MethodResultClass>(
	    this DataPacket dataPacket)
	{
		var dataPacketTextEncoding
		    = dataPacket.TextEncoding;

		////////////////////////////////////////////////
		// 1/3，响应信息基础字段：
		////////////////////////////////////////////////
		var apiResponseInfoDataSection
		    = dataPacket.GetDataPacketSectionWithSectionName(
		    DataPacketSectionNames.ApiResponseInfo);
		var apiResponseInfo
		    = apiResponseInfoDataSection?.GetObjectValue<ApiResponseInfo<MethodResultClass>>(
		    dataPacketTextEncoding);
		if (apiResponseInfo == null)
		{
			return null;
		}

		////////////////////////////////////////////////
		// 2/3，方法响应结果参数（基本）：
		////////////////////////////////////////////////
		var apiResponseMethodParamsDataSection
		    = dataPacket.GetDataPacketSectionWithSectionName(
		    DataPacketSectionNames.ApiResponseInfo_MethodParams);
		if (apiResponseMethodParamsDataSection != null)
		{
			apiResponseInfo.Result
			    = apiResponseMethodParamsDataSection.GetObjectValue<MethodResultClass>(
			    dataPacketTextEncoding);
		}

		////////////////////////////////////////////////
		// 3/3，方法响应结果参数（扩展）：
		////////////////////////////////////////////////
		foreach (var dataPacketSection in dataPacket)
		{
			if (DataPacketSectionNames.ApiResponseInfo.EqualsIgnoreCase(dataPacketSection.Name)
			    == false
			    && DataPacketSectionNames.ApiResponseInfo_MethodParams.EqualsIgnoreCase(dataPacketSection.Name)
			    == false)
			{
				apiResponseInfo.AddExtensionParam(dataPacketSection);
			}
		}
		return apiResponseInfo;
	}

	#endregion
}
