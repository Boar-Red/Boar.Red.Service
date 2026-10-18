using BaoXia.Constants;
using BaoXia.Utils.Interfaces;
using BaoXia.Utils.Models;
using Boar.Red.Service.BrService.Constants;
using Boar.Red.Service.BrService.Extensions;
using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.BrService.ViewModels;

namespace Boar.Red.Service.BrService;

public class AdministratorClient(string? groupName, string serviceName, string? serviceUri, ServiceDiscoverer? serviceDiscoverer,
    string? applicationIdentity, string? applicationPassword, string? applicationAesKey,
    Func<ClientConfig?>? toGetClientConfig, Func<int, ClientIpInfo, ServiceAdministratorLoginRequest>? toGetServiceAdministratorLoginRequest,
    ILogFile? logFile, ILogFile? exceptionLogFile, ILogFile? warningLogFile, ILogFile? bxServiceLogFile)
    : Client(groupName, serviceName, serviceUri, serviceDiscoverer, applicationIdentity, applicationPassword, applicationAesKey, toGetClientConfig,
    logFile, exceptionLogFile, warningLogFile, bxServiceLogFile)
{

	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

	public Func<int, ClientIpInfo, ServiceAdministratorLoginRequest>? ToGetServiceAdministratorLoginRequest { get; set; } = toGetServiceAdministratorLoginRequest;


	private string? _serviceAdministratorSessionToken = null;

	public string? ServiceAdministratorSessionToken => _serviceAdministratorSessionToken;


	private DateTimeOffset _serviceAdministratorSessionTokenUpdateTime = DateTimeOffset.Now;

	public DateTimeOffset ServiceAdministratorSessionTokenUpdateTime => _serviceAdministratorSessionTokenUpdateTime;


	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public async Task<ServiceAdministratorLoginResult> TryUpdateServiceAdministratorSessionTokenAsync(
	    int clientUesrId, ClientIpInfo clientIpInfo, bool isUpdateForced = false)
	{
		var serviceAdministratorSessionToken = _serviceAdministratorSessionToken ?? string.Empty;
		if (!string.IsNullOrEmpty(serviceAdministratorSessionToken)
		    && !isUpdateForced)
		{
			return new()
			{
				SessionToken = serviceAdministratorSessionToken
			};
		}

		ServiceAdministratorLoginRequest serviceAdministratorLoginRequest;
		if (ToGetServiceAdministratorLoginRequest != null)
		{
			serviceAdministratorLoginRequest = ToGetServiceAdministratorLoginRequest(clientUesrId, clientIpInfo);
		}
		else
		{
			var config = Config;
			var serviceAdminAccount = config?.ServiceAdministratorAccount ?? string.Empty;
			var serviceAdminPassword = config?.ServiceAdministratorPassword ?? string.Empty;
			serviceAdministratorLoginRequest = new ServiceAdministratorLoginRequest(
			    serviceAdminAccount, serviceAdminPassword, clientUesrId, clientIpInfo);
		}

		var loginResponseInfo
		    = await InvokeApiToGetObjectAsync<ServiceAdministratorLoginResponse>(
		    "serviceAdministrator/login",
		    serviceAdministratorLoginRequest);
		var loginErrorResult = loginResponseInfo.ToErrorResult("获取服务管理员会话令牌失败");
		if (loginErrorResult != null)
		{
			return new()
			{
				Error = loginErrorResult.Error,
				ErrorDescription = loginErrorResult.ErrorDescription
			};
		}

		var administratorSessionToken = loginResponseInfo.Result?.SessionToken;
		if (string.IsNullOrEmpty(administratorSessionToken))
		{
			return new()
			{
				Error = Error.OperationFailed,
				ErrorDescription = "获取到的服务管理员会话令牌为”空“。"
			};
		}

		// !!!
		_serviceAdministratorSessionToken = administratorSessionToken;
		_serviceAdministratorSessionTokenUpdateTime = DateTime.UtcNow;
		// !!!

		return new()
		{
			SessionToken = _serviceAdministratorSessionToken
		};
	}

	public async Task<ApiResponseInfo<MethodResultType>?> GetErrorResultToSetRequestServiceAdminSessionTokenAsync
	    <MethodResultType>
	    (ServiceAdministratorRequest adminRequest,
	    bool isServiceAdministratorSessionTokenUpdateForced = false)
	{
		var updateServiceAdministratorSessionTokenResult = await TryUpdateServiceAdministratorSessionTokenAsync(
		    adminRequest.ClientUserId, adminRequest.ClientIpInfo, isServiceAdministratorSessionTokenUpdateForced);
		if (updateServiceAdministratorSessionTokenResult.IsFailed)
		{
			return new()
			{
				StatusCode = (int)System.Net.HttpStatusCode.Unauthorized,
				StatusDescription = "无法获取服务管理员会话令牌：" + updateServiceAdministratorSessionTokenResult.ToString()
			};
		}

		// !!!
		adminRequest.ServiceAdministratorSessionToken = updateServiceAdministratorSessionTokenResult.SessionToken;
		// !!!

		return null;
	}

	public async Task<ApiResponseInfo<MethodResultType>> InvokeAdminApiToGetApiResponseInfoAsync<MethodResultType>(
	    string? methodName,
	    ServiceAdministratorRequest methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams,
	    DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.None,
	    string? apiRequestIdentity = null)
	{
		var errorResultToSetRequestAdminSessionToken
		    = await GetErrorResultToSetRequestServiceAdminSessionTokenAsync
		    <MethodResultType>
		    (methodParams);
		if (errorResultToSetRequestAdminSessionToken != null)
		{
			return errorResultToSetRequestAdminSessionToken;
		}
		var apiResponseInfo
		    = await InvokeApiToGetApiResponseInfoAsync
		    <MethodResultType>
		    (methodName,
		    methodParams,
		    methodExtensionParams,
		    encryptionMethod,
		    apiRequestIdentity);
		if (apiResponseInfo.IsFailed
		    && apiResponseInfo.StatusCode == (int)System.Net.HttpStatusCode.Unauthorized)
		{
			errorResultToSetRequestAdminSessionToken
			= await GetErrorResultToSetRequestServiceAdminSessionTokenAsync
			<MethodResultType>
			(methodParams, true);
			if (errorResultToSetRequestAdminSessionToken != null)
			{
				return errorResultToSetRequestAdminSessionToken;
			}
			apiResponseInfo
			    = await InvokeApiToGetApiResponseInfoAsync
			    <MethodResultType>
			    (methodName,
			    methodParams,
			    methodExtensionParams,
			    encryptionMethod,
			    apiRequestIdentity);
		}
		return apiResponseInfo;
	}

	public async Task<ApiResponseInfo<object>> InvokeAdminApiToGetBytesAsync(
	    string? methodName,
	    ServiceAdministratorRequest methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams,
	    DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.None)
	{
		var errorResultToSetRequestAdminSessionToken
		    = await GetErrorResultToSetRequestServiceAdminSessionTokenAsync
		    <object>
		    (methodParams);
		if (errorResultToSetRequestAdminSessionToken != null)
		{
			return errorResultToSetRequestAdminSessionToken;
		}
		var apiResponseInfo
		    = await InvokeApiToGetBytesAsync(
		    methodName,
		    methodParams,
		    methodExtensionParams,
		    encryptionMethod);
		if (apiResponseInfo.IsFailed
		    && apiResponseInfo.StatusCode == (int)System.Net.HttpStatusCode.Unauthorized)
		{
			errorResultToSetRequestAdminSessionToken
			= await GetErrorResultToSetRequestServiceAdminSessionTokenAsync
			<object>
			(methodParams, true);
			if (errorResultToSetRequestAdminSessionToken != null)
			{
				return errorResultToSetRequestAdminSessionToken;
			}
			apiResponseInfo
			    = await InvokeApiToGetBytesAsync(
			    methodName,
			    methodParams,
			    methodExtensionParams,
			    encryptionMethod);
		}
		return apiResponseInfo;
	}

	public async Task<ApiResponseInfo<string>> InvokeAdminApiToGetStringAsync(
	    string? methodName,
	    ServiceAdministratorRequest methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams,
	    DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.None)
	{
		var errorResultToSetRequestAdminSessionToken
		    = await GetErrorResultToSetRequestServiceAdminSessionTokenAsync
		    <string>
		    (methodParams);
		if (errorResultToSetRequestAdminSessionToken != null)
		{
			return errorResultToSetRequestAdminSessionToken;
		}
		var apiResponseInfo
		    = await InvokeApiToGetStringAsync(
		    methodName,
		    methodParams,
		    methodExtensionParams,
		    encryptionMethod);
		if (apiResponseInfo.IsFailed
		    && apiResponseInfo.StatusCode == (int)System.Net.HttpStatusCode.Unauthorized)
		{
			errorResultToSetRequestAdminSessionToken
			= await GetErrorResultToSetRequestServiceAdminSessionTokenAsync
			<string>
			(methodParams, true);
			if (errorResultToSetRequestAdminSessionToken != null)
			{
				return errorResultToSetRequestAdminSessionToken;
			}
			apiResponseInfo
			    = await InvokeApiToGetStringAsync(
			    methodName,
			    methodParams,
			    methodExtensionParams,
			    encryptionMethod);
		}
		return apiResponseInfo;
	}

	public async Task<ApiResponseInfo<ObjectType>> InvokeAdminApiToGetObjectAsync
	    <ObjectType>
	    (string? methodName,
	    ServiceAdministratorRequest methodParams,
	    IEnumerable<DataPacketSection>? methodExtensionParams,
	    DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.None)
	{
		var errorResultToSetRequestAdminSessionToken
		    = await GetErrorResultToSetRequestServiceAdminSessionTokenAsync
		    <ObjectType>
		    (methodParams);
		if (errorResultToSetRequestAdminSessionToken != null)
		{
			return errorResultToSetRequestAdminSessionToken;
		}
		var apiResponseInfo
		    = await InvokeApiToGetObjectAsync<ObjectType>(
		    methodName,
		    methodParams,
		    methodExtensionParams,
		    encryptionMethod);
		if (apiResponseInfo.IsFailed
		    && apiResponseInfo.StatusCode == (int)System.Net.HttpStatusCode.Unauthorized)
		{
			errorResultToSetRequestAdminSessionToken
			= await GetErrorResultToSetRequestServiceAdminSessionTokenAsync
			<ObjectType>
			(methodParams, true);
			if (errorResultToSetRequestAdminSessionToken != null)
			{
				return errorResultToSetRequestAdminSessionToken;
			}
			apiResponseInfo
			    = await InvokeApiToGetObjectAsync<ObjectType>(
			    methodName,
			    methodParams,
			    methodExtensionParams,
			    encryptionMethod);
		}
		return apiResponseInfo;
	}

	public async Task<ApiResponseInfo<ObjectType>> InvokeAdminApiToGetObjectAsync
	    <ObjectType>
	    (string? methodName,
	    ServiceAdministratorRequest methodParams,
	    DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.None,
	    params DataPacketSection[] methodExtensionParams)
	{
		var errorResultToSetRequestAdminSessionToken = await GetErrorResultToSetRequestServiceAdminSessionTokenAsync<ObjectType>
		    (methodParams);
		if (errorResultToSetRequestAdminSessionToken != null)
		{
			return errorResultToSetRequestAdminSessionToken;
		}
		var apiResponseInfo
		    = await InvokeApiToGetObjectAsync<ObjectType>(
		    methodName,
		    methodParams,
		    encryptionMethod,
		    methodExtensionParams);
		if (apiResponseInfo.IsFailed
		    && apiResponseInfo.StatusCode == (int)System.Net.HttpStatusCode.Unauthorized)
		{
			errorResultToSetRequestAdminSessionToken
			= await GetErrorResultToSetRequestServiceAdminSessionTokenAsync
			<ObjectType>
			(methodParams, true);
			if (errorResultToSetRequestAdminSessionToken != null)
			{
				return errorResultToSetRequestAdminSessionToken;
			}
			apiResponseInfo
			    = await InvokeApiToGetObjectAsync<ObjectType>(
			    methodName,
			    methodParams,
			    encryptionMethod,
			    methodExtensionParams);
		}
		return apiResponseInfo;
	}

	#endregion
}