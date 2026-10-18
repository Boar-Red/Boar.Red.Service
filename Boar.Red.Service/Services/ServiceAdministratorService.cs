using BaoXia.Constants.Models;
using BaoXia.Utils;
using BaoXia.Utils.Cache;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Interfaces;
using BaoXia.Utils.Models;
using Boar.Red.Service.BrService.ViewModels;
using Boar.Red.Service.Constants;
using Boar.Red.Service.Models;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Error = BaoXia.Constants.Error;

namespace Boar.Red.Service.Services;

// [Service(ServiceLifeCycle.Singleton)]
// ServiceAdministratorService 使用手动配置，无需自动配置。
public class ServiceAdministratorService
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	private readonly ConfigFile? _configFile;

	private readonly PropertyInfo? _administratorConfigPropertyInfoInConfigFile;

	public ServiceAdministratorConfig Config
	{
		get
		{
			ServiceAdministratorConfig? administratorConfig = null;
			if (_configFile != null
				&& _administratorConfigPropertyInfoInConfigFile != null)
			{
				administratorConfig
					= _administratorConfigPropertyInfoInConfigFile.GetValue(_configFile)
					as ServiceAdministratorConfig;
			}
			administratorConfig ??= ServiceAdministratorConfig.Default;
			return administratorConfig;
		}
	}


	public ILogFile? LogFile { get; set; }
	public ILogFile? ExceptionLogFile { get; set; }
	public ILogFile? WarningLogFile { get; set; }



	protected readonly ItemsCache<string, ServiceAdministratorSession, object> _sessions;

	#endregion

	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ServiceAdministratorService(
		ConfigFile configFile,
		string administratorConfigPropertyNameInConfigFile,
		//
		ILogFile? logFile,
		ILogFile? exceptionLogFile,
		ILogFile? warningLogFile)
	{
		////////////////////////////////////////////////
		// @配置文件，初始化
		////////////////////////////////////////////////

		_configFile = configFile;
		var administratorConfigPropertyInfoInConfigFile
			= _configFile
			.GetType()
			.GetProperty(administratorConfigPropertyNameInConfigFile);
		if (administratorConfigPropertyInfoInConfigFile == null)
		{
			var serviceConfigPropertyException
				= new ArgumentException(
				$"无法在配置文件中，使用名称“{nameof(administratorConfigPropertyNameInConfigFile)}”找到服务管理员配置（ServiceAdministratorService）的属性信息。",
				nameof(administratorConfigPropertyNameInConfigFile));
			{
				serviceConfigPropertyException.Data.Add(
					nameof(configFile), configFile);
				serviceConfigPropertyException.Data.Add(
					nameof(administratorConfigPropertyNameInConfigFile), administratorConfigPropertyNameInConfigFile);
			}
			throw serviceConfigPropertyException;
		}
		_administratorConfigPropertyInfoInConfigFile = administratorConfigPropertyInfoInConfigFile;



		////////////////////////////////////////////////
		// @日志文件，初始化
		////////////////////////////////////////////////

		LogFile = logFile;
		ExceptionLogFile = exceptionLogFile;
		WarningLogFile = warningLogFile;

		_sessions = new(
		(sessionToken, _) =>
		{
			return null;
		},
		null,
		null,
		() =>
		{
			var sessionRetainSeconds = Config.SessionRetainSeconds;
			if (sessionRetainSeconds <= 0.0)
			{
				sessionRetainSeconds = ServiceAdministratorConfig.SessionLiveSecondsMaxDefault;
			}
			return sessionRetainSeconds;
		},
		null);
	}

	public ServiceAdministratorSession? GetSessionWithToken(string? sessionToken)
	{
		if (string.IsNullOrEmpty(sessionToken))
		{
			return null;
		}

		_sessions.TryGet(
			sessionToken,
			out var session);
		{ }
		return session;
	}

	public ResultWithObject<ServiceAdministratorSession> GetValidSessionWithToken(string? sessionToken, string? clientConnectionKeyAddress)
	{
		if (string.IsNullOrEmpty(sessionToken))
		{
			return new()
			{
				Error = Error.InvalidRequest,
				ErrorDescription = $"指定的会话令牌（{nameof(sessionToken)}）为“空”。"
			};
		}

		var isClientIPValid = false;
		var administratorIPAddresses = Config.AdministratorIPAddresses;
		// 不指定管理员IP地址信息时，视为不限制IP地址：
		if (ArrayUtil.IsEmpty(administratorIPAddresses))
		{
			// !!!
			isClientIPValid = true;
			// !!!
		}
		else if (string.IsNullOrEmpty(clientConnectionKeyAddress) != true)
		{
			foreach (var administratorIPAddress in administratorIPAddresses)
			{
				if (clientConnectionKeyAddress.EqualsIgnoreCase(administratorIPAddress) == true)
				{
					// !!!
					isClientIPValid = true;
					break;
					// !!!
				}
			}
		}
		if (!isClientIPValid)
		{
			// !!!
			WarningLogFile?.Logs(this, $"使用“请求对象”获取会话令牌失败，无效的客户端IP地址：{clientConnectionKeyAddress}。", null);
			// !!!
			return new()
			{
				Error = Error.ClientAddressInvalid,
				ErrorDescription = $"客户端的Ip地址（{clientConnectionKeyAddress}）不在允许范围内。"
			};
			// !!!
		}


		_sessions.TryGet(sessionToken, out var session);
		if (session == null)
		{
			return new()
			{
				Error = Error.UserTokenNotExisted,
				ErrorDescription = "指定的会话不存在。"
			};
		}

		var sessionLiveSeconds = (DateTime.Now - session.CreateTime).TotalSeconds;
		if (sessionLiveSeconds > Config.SessionRetainSeconds)
		{
			return new()
			{
				Error = Error.UserTokenExpired,
				ErrorDescription = "指定的会话已过期。"
			};
		}
		return new()
		{
			ResultObject = session
		};
	}

	public ServiceAdministratorSession? GetSessionWithRequest(
		HttpRequest? request, out System.Net.HttpStatusCode responseStatusCode, out string error, out string? errorDescription)
	{
		// !!!
		responseStatusCode = System.Net.HttpStatusCode.OK;
		error = Error.None;
		errorDescription = null;
		// !!!

		if (request == null)
		{
			// !!!
			ExceptionLogFile?.Logs(
				this,
				"使用“请求对象”获取会话令牌失败，无法获取请求对象。",
				null);
			// !!!
			responseStatusCode = System.Net.HttpStatusCode.InternalServerError;
			error = Error.OperationFailed;
			errorDescription = "基础参数无效，无法获取请求对象。";
			// !!!
			return null;
		}

		var isClientIPValid = false;
		var administratorIPAddresses = Config.AdministratorIPAddresses;
		// 不指定管理员IP地址信息时，视为不限制IP地址：
		if (ArrayUtil.IsEmpty(administratorIPAddresses))
		{
			// !!!
			isClientIPValid = true;
			// !!!
		}
		else
		{
			var connectionIpEndPoints = request.GetConnectionIpEndPoints();
			//if (ListUtil.IsEmpty(clientConnectionAddressList))
			//{
			//	// !!!
			//	ExceptionLogFile?.Logs(this, "使用“请求对象”获取会话令牌失败，无法获取客户端IP地址。", null);
			//	// !!!
			//	responseStatusCode = System.Net.HttpStatusCode.InternalServerError;
			//	error = Error.OperationFailed;
			//	errorDescription = "基础参数无效，无法获取客户端IP地址。";
			//	// !!!
			//	return null;
			//}
			foreach (var administratorIPAddress in administratorIPAddresses)
			{
				if (connectionIpEndPoints.Contains(administratorIPAddress))
				{
					// !!!
					isClientIPValid = true;
					break;
					// !!!
				}
			}
		}
		if (!isClientIPValid)
		{
			var clientIpAddresseChainString = request.GetClientConnectionAddressInfesString();
			// !!!
			WarningLogFile?.Logs(
				this,
				$"使用“请求对象”获取会话令牌失败，无效的客户端IP地址：{clientIpAddresseChainString}。",
				null);
			// !!!
			responseStatusCode = System.Net.HttpStatusCode.Unauthorized;
			error = Error.ClientAddressInvalid;
			errorDescription = $"客户端IP地址无效，当前客户端IP地址为：{clientIpAddresseChainString}。";
			// !!!
			return null;
		}

		var sessionToken = request.Headers[HttpHeaderKeys.ServiceAdministrator.SessionToken];
		if (string.IsNullOrEmpty(sessionToken))
		{
			sessionToken = request.Cookies[CookieNames.ServiceAdministrator.SessionToken];
		}
		if (string.IsNullOrEmpty(sessionToken))
		{
			// !!!
			WarningLogFile?.Logs(
				this,
				"使用“请求对象”获取会话令牌失败，无法获取到“管理员会话令牌”。",
				null);
			// !!!
			responseStatusCode = System.Net.HttpStatusCode.Unauthorized;
			error = Error.UserTokenNotExisted;
			errorDescription = "管理员会话令牌无效，无法获取到“管理员会话令牌”。";
			// !!!
			return null;
		}

		var session = GetSessionWithToken(sessionToken);
		if (session == null)
		{
			// !!!
			WarningLogFile?.Logs(
				this,
				"使用“请求对象”获取会话令牌失败，无法获取到指定的“管理员会话”。",
				null);
			// !!!
			responseStatusCode = System.Net.HttpStatusCode.Unauthorized;
			error = Error.UserTokenNotExisted;
			errorDescription = "管理员会话令牌无效，无法找到指定的“管理员会话”。";
			// !!!
			return null;
		}
		var sessionLiveSeconds = (DateTime.Now - session.CreateTime).TotalSeconds;
		if (sessionLiveSeconds > Config.SessionRetainSeconds)
		{
			// !!!
			WarningLogFile?.Logs(
				this,
				"使用“请求对象”获取会话令牌失败，指定的“管理员会话”已失效。",
				null);
			// !!!
			responseStatusCode = System.Net.HttpStatusCode.Unauthorized;
			error = Error.UserTokenExpired;
			errorDescription = "管理员会话令牌无效，指定的“管理员会话”已失效。";
			// !!!
			return null;
		}

		return session;
	}

	public Result? GetErrorResultForNeedAdministratorSessionWithRequest(ServiceAdministratorRequest? request)
	{
		if (request == null)
		{
			return new Result
			{
				Error = Error.InvalidRequest,
				ErrorDescription = "服务管理员请求为“null”。"
			};
		}

		var getAdminSessionResult = GetValidSessionWithToken(request.ServiceAdministratorSessionToken, request.ClientIpInfo.KeyIpAddress);
		if (getAdminSessionResult?.IsFailed == true)
		{
			return getAdminSessionResult;
		}
		return null;
	}

	public Response? GetErrorResponseForNeedAdministratorSessionWithRequest(
		ServiceAdministratorRequest? request)
	{
		var errorResult
			= GetErrorResultForNeedAdministratorSessionWithRequest(
			request);
		if (errorResult?.IsFailed == true)
		{
			return new Response(errorResult);
		}
		return null;
	}

	protected void LogsWarningForAdministratorLogin(
		string logContent, string? administratorAccount, ConnectionIpEndPoints? connectionIpEndPoints)
	{
		var administratorClientConnectionTitle = connectionIpEndPoints?.IpEndPoints?.ToClientConnectionIpEndPointsString();
		WarningLogFile?.Logs(this,
			$"{logContent}，登录信息为：\r\n账号：{administratorAccount}；\r\n客户端地址：{administratorClientConnectionTitle}。", null);
	}

	public string Login(string? administratorAccount, string? administratorPassword, ConnectionIpEndPoints? connectionIpEndPoints,
		out ServiceAdministratorSession? administratorSession)
	{
		// !!!
		administratorSession = null;
		// !!!

		var isClientIPValid = false;
		if (connectionIpEndPoints != null)
		{
			var administratorIPAddressInfes = Config.AdministratorIPAddressInfes;
			if (administratorIPAddressInfes?.Length > 0)
			{
				foreach (var administratorIPAddressInfo in administratorIPAddressInfes)
				{
					if (administratorIPAddressInfo.IPString is { } ipString && connectionIpEndPoints.Contains(ipString))
					{
						isClientIPValid = true;
						break;
					}
				}
			}
			else
			{
				isClientIPValid = true;
			}
		}
		if (!isClientIPValid)
		{
			// !!!
			LogsWarningForAdministratorLogin("服务管理员登录失败，客户端IP地址无效", administratorAccount, connectionIpEndPoints);
			// !!!
			return Error.ClientAddressInvalid;
		}


		if (string.IsNullOrEmpty(administratorAccount))
		{
			// !!!
			LogsWarningForAdministratorLogin("服务管理员登录失败，使用了空的管理员账号", administratorAccount, connectionIpEndPoints);
			// !!!
			return Error.UserAccountInvalid;
		}
		if (string.IsNullOrEmpty(administratorPassword))
		{
			// !!!
			LogsWarningForAdministratorLogin("服务管理员登录失败，使用了空的管理员密码", administratorAccount, connectionIpEndPoints);
			// !!!
			return Error.UserPasswordInvalid;
		}

		var administrators = Config.Administrators;
		if (administrators == null
			|| administrators.Length < 1)
		{
			// !!!
			LogsWarningForAdministratorLogin("服务管理员登录失败，未配置管理员", administratorAccount, connectionIpEndPoints);
			// !!!
			return Error.ObjectNotExisted;
		}

		var isAccountExisted = false;
		foreach (var administrator in administrators)
		{
			if (administrator.Account?.EqualsIgnoreCase(administratorAccount) == true)
			{
				if (administrator.IsValid)
				{
					isAccountExisted = true;
					if (administratorPassword.Equals(administrator.Password))
					{
						// !!!
						// 1/2，注销之前的登录信息：
						var sessionsCountMax = Config.SessionsCountMax;
						var allSessionInfoContainers = _sessions.ValueContainers;
						if (sessionsCountMax > 0
							&& allSessionInfoContainers?.Count > 0)
						{
							var currentAccountSessionInfoContainers
								= new List<ItemCacheItemContainer<string, ServiceAdministratorSession?, object?>>();
							foreach (var sessionInfoContainer in allSessionInfoContainers)
							{
								if (sessionInfoContainer != null)
								{
									var sessionInfo = sessionInfoContainer.Item;
									if (administratorAccount.EqualsIgnoreCase(sessionInfo?.Account) == true)
									{
										// !!!
										currentAccountSessionInfoContainers.InsertWithOrder(
											sessionInfoContainer,
											(sessionInfoContainerA, sessionInfoContainerB) =>
											{
												var sessionInfoA = sessionInfoContainerA.Item;
												var sessionInfoB = sessionInfoContainerB.Item;
												if (sessionInfoA == null)
												{
													return 1;
												}
												if (sessionInfoB == null)
												{
													return -1;
												}
												return sessionInfoB.CreateTime.CompareTo(sessionInfoA.CreateTime);
											});
										// !!!
									}
								}
							}
							while ((currentAccountSessionInfoContainers.Count + 1) > sessionsCountMax)
							{
								// !!!
								var currentAccountSessionTokenNeedRemoved = currentAccountSessionInfoContainers[^1].Key;
								currentAccountSessionInfoContainers.RemoveAt(currentAccountSessionInfoContainers.Count - 1);
								_sessions.Remove(currentAccountSessionTokenNeedRemoved);
								// !!!
							}
						}
						// 2/2，开始新的会话：
						var newSession = new ServiceAdministratorSession(administratorAccount, administratorPassword,
							connectionIpEndPoints?.IpEndPoints?.ToClientConnectionIpEndPointsString(), DateTime.Now);
						_sessions.Add(newSession.Token, newSession);
						// !!!
						administratorSession = newSession;
						// !!!
					}
					else
					{
						// !!!
						LogsWarningForAdministratorLogin(
							"服务管理员登录失败，管理员密码错误", administratorAccount, connectionIpEndPoints);
						// !!!
						return Error.UserPasswordInvalid;
					}
				}
				break;
			}
		}
		if (!isAccountExisted)
		{
			// !!!
			LogsWarningForAdministratorLogin("服务管理员登录失败，管理员账号错误", administratorAccount, connectionIpEndPoints);
			// !!!
			return Error.UserAccountNotExisted;
		}
		return Error.None;
	}

	public string Logout(string? administratorSessionToken)
	{
		if (string.IsNullOrEmpty(administratorSessionToken))
		{
			return Error.InvalidRequest;
		}

		// 响应业务代码。
		var administratorSessionTokenRemoved = _sessions.Remove(administratorSessionToken!);
		if (!administratorSessionToken.Equals(administratorSessionTokenRemoved?.Token))
		{
			return Error.ObjectNotExisted;
		}
		return Error.None;
	}

	#endregion
}
