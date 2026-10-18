
using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.Middlewares;
using Boar.Red.Service.BrService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace Boar.Red.Service.BrService.Controllers;

public class BrClientConnectionInfoController(BrServiceMiddleware bxServiceMiddleware) : Controller
{
	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

	private static readonly ConcurrentDictionary<int, ClientConnectionInfo> _clientConnectionInfes = new();

	public static ICollection<ClientConnectionInfo> ClientConnectionInfes
	{
		get
		{
			return _clientConnectionInfes.Values;
		}
	}

	private static int _clientConnectionId = 0;

	#endregion


	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public BrServiceMiddleware _bxService = bxServiceMiddleware;

	#endregion


	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static int GenerateClientConnectionId()
	{
		return Interlocked.Increment(ref _clientConnectionId);
	}

	public static ClientConnectionInfo? GetClientConnectionInfoWithId(int clientConnectionId)
	{
		if (clientConnectionId == 0)
		{
			return null;
		}

		if (!_clientConnectionInfes.TryGetValue(
		    clientConnectionId,
		    out var clientConnectionInfo))
		{
			return null;
		}
		return clientConnectionInfo;
	}

	public static bool TryRemoveClientConnectionInfo(
	    int clientConnectionInfoId)
	{
		if (clientConnectionInfoId == 0)
		{
			return false;
		}

		var clientConnectionInfo
		    = GetClientConnectionInfoWithId(clientConnectionInfoId);
		if (clientConnectionInfo == null)
		{
			return false;
		}

		// !!!
		_clientConnectionInfes.TryRemove(clientConnectionInfoId, out _);
		// !!!
		return true;
	}

	public static string TryCloseClientConnectionWithId(int clientConnectionId)
	{
		var clientConnection = GetClientConnectionInfoWithId(clientConnectionId);
		if (clientConnection == null)
		{
			return Error.ObjectNotExisted;
		}

		clientConnection.SetClose();

		return Error.None;
	}

	public static bool TryRemoveClientConnectionInfo(
	    ClientConnectionInfo? clientConnectionInfo)
	{
		if (clientConnectionInfo == null)
		{
			return false;
		}
		return TryRemoveClientConnectionInfo(
		    clientConnectionInfo.Id);
	}

	#endregion



	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public class OpenConnectRequest : Request
	{
		/// <summary>
		/// 客户端已有的连接Id。
		/// </summary>
		public int ClientConnectionId { get; set; }

		/// <summary>
		/// 客户端版本号。
		/// </summary>
		public string? ClientVersion { get; set; }

		/// <summary>
		/// 当前调用服务的应用程序Id。
		/// </summary>
		public string? ApplicationIdentity { get; set; }

		/// <summary>
		/// 当前调用服务的应用程序密码。
		/// </summary>
		public string? ApplicationPassword { get; set; }

		public OpenConnectRequest()
		{ }

		public OpenConnectRequest(
		    int clientConnectionId,
		    string? clientVersion,
		    string? applicationIdentity,
		    string? applicationPassword)
		{
			ClientConnectionId = clientConnectionId;
			ClientVersion = clientVersion;
			ApplicationIdentity = applicationIdentity;
			ApplicationPassword = applicationPassword;
		}

		protected override BaoXia.Constants.Models.Response? DidGetErrorResonseByCheckRequest(RequestPipelineType requestPipelineType)
		{
			var errorResponse = base.DidGetErrorResonseByCheckRequest(requestPipelineType);
			if (errorResponse?.IsFailed == true)
			{
				return errorResponse;
			}
			if (string.IsNullOrWhiteSpace(ApplicationIdentity))
			{
				return new()
				{
					Error = Error.InvalidRequest,
					ErrorDescription = $"请求无效，“{nameof(ApplicationIdentity)}”不能为“空”。"
				};
			}
			return null;
		}
	}

	public class OpenConnectResponse : Response
	{
		public int ConnectionId { get; set; }

		public ServiceBaseInfo? ServiceInfo { get; set; }

		public ApplicationInfo? ApplicationInfo { get; set; }

		public ClientConfig? ClientConfig { get; set; }
	}

	public IActionResult OpenConnection(OpenConnectRequest request)
	{
		var response = new OpenConnectResponse();
		try
		{
			if (_bxService.DidIsClientVersionValid(request.ClientVersion))
			{
				ClientConnectionInfo? clientConnectionInfoMatched = null;
				ApplicationInfo? applicationInfoMatched = null;
				var requestClientConnectionId = request.ClientConnectionId;
				if (requestClientConnectionId != 0)
				{
					clientConnectionInfoMatched
					    = GetClientConnectionInfoWithId(
					    request.ClientConnectionId);
					applicationInfoMatched
					    = clientConnectionInfoMatched?.ApplicationInfo;
				}
				////////////////////////////////////////////////
				// !!! ⚠
				// 某些情况下，客户端的所有“Socket”都已关闭，导致服务端已清空了该请求信息（ClientConnectionInfo），
				// 而客户的“ClientConnection”由于某种原因没有销毁，导致本次请求的“requestClientConnectionId”，没有对应的请求信息（ClientConnectionInfo），
				// 这种清空下，就需要使用请求的“ApplicationIdentity”字段，重新登录，但是【复用客户端的请求Id】重建请求信息（ClientConnectionInfo）。
				// !!! ⚠
				////////////////////////////////////////////////
				applicationInfoMatched
					??= _bxService.GetApplicationInfoWithApplicationIdentity(
					request.ApplicationIdentity);

				if (applicationInfoMatched != null)
				{
					var now = DateTime.Now;
					if (applicationInfoMatched.ExpireTime <= now)
					{
						response.Error = Error.UserAccountExpired;
						response.ErrorDescription = "打开连接失败，应用程序账号已过期。";
					}
					else if (string.IsNullOrEmpty(applicationInfoMatched.Password)
					    || applicationInfoMatched.Password.Equals(request.ApplicationPassword))
					{
						var clientConnectionInfo = clientConnectionInfoMatched;
						if (clientConnectionInfo == null)
						{
							var clientConnectionInfoId
							    = requestClientConnectionId > 0
							    ? requestClientConnectionId
							    : GenerateClientConnectionId();
							clientConnectionInfo
							    = new ClientConnectionInfo(
							    clientConnectionInfoId,
							    applicationInfoMatched)
							    {
								    ConnectTime = now,
								    DisconnectTime = DateTime.MaxValue,
								    LastInvokeTime = now
							    };
							_clientConnectionInfes.AddOrUpdateWithNewValue(
							    clientConnectionInfo.Id,
							    clientConnectionInfo);
						}

						var applicationInfoCloned = new ApplicationInfo();
						{
							applicationInfoCloned.SetPropertiesWithSameNameFrom(
							    applicationInfoMatched,
							    nameof(ApplicationInfo.Password));
						}

						if (response.ConnectionId == 0)
						{
							// !!!
							response.ConnectionId = clientConnectionInfo.Id;
							// !!!
						}

						// !!!
						response.ApplicationInfo = applicationInfoCloned;
						// !!!
						var serviceConfig = _bxService.Config;
						var clientConfig = new ClientConfig();
						{
							clientConfig.ClientRequestDataPacketSizeToCompression
							    = serviceConfig.ClientRequestDataPacketSizeToCompression;
							clientConfig.ClientRequestDataPacketCompressionMethodName
							    = serviceConfig.ClientRequestDataPacketCompressionMethodName;

							clientConfig.ServiceResponseTextEncodingName
							    = serviceConfig.ServiceResponseTextEncodingName;
							clientConfig.ClientRequestTextEncodingName
							    = serviceConfig.ClientRequestTextEncodingName;
						}
						response.ClientConfig = clientConfig;
						// !!!
					}
					else
					{
						response.Error = Error.UserPasswordInvalid;
						response.ErrorDescription = "打开连接失败，应用程序密码错误。";
					}
				}
				else
				{
					response.Error = Error.UserAccountNotExisted;
					response.ErrorDescription = "打开连接失败，应用程序账号不存在。";
				}
			}
			else
			{
				response.Error = Error.UnsupportedRequestVersion;
				response.ErrorDescription = "打开连接失败，无效的请求版本。";
			}
		}
		catch (Exception exception)
		{
			var errorDescription = "应用程序客户端打开连接失败，程序异常。";
			//
			_bxService
			    .ExceptionLogFile
			    ?.Logs(this, errorDescription, exception);
			//
			response.Error = Error.ProgramError;
			response.ErrorDescription = errorDescription;
		}
		return Json(response);
	}

	public class CloseConnectRequest
	{
		public int ConnectionId { get; set; }

		public bool IsValid
		{
			get
			{
				if (this.ConnectionId != 0)
				{
					return true;
				}
				return false;
			}
		}
	}

	public IActionResult CloseConnection(CloseConnectRequest request)
	{
		var response = new Response();
		try
		{
			if (request?.IsValid == true)
			{
				response.Error = TryCloseClientConnectionWithId(request.ConnectionId);
			}
			else
			{
				response.Error = Error.InvalidRequest;
			}
		}
		catch (Exception exception)
		{
			_bxService
			    .ExceptionLogFile
			    ?.Logs(this, "应用程序客户端关闭连接失败，程序异常。", exception);
			//
			response.Error = Error.ProgramError;
		}
		return Json(response);
	}

	#endregion
}
