using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils;
using BaoXia.Utils.ConcurrentTools;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Models;
using Boar.Red.Service.Attributes;
using Boar.Red.Service.BrService.Interfaces;
using Boar.Red.Service.BrService.Middlewares;
using Boar.Red.Service.BrService.ViewModels;
using Boar.Red.Service.WebService.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;

namespace Boar.Red.Service.BrService.Services;

public abstract class WebSocketService<TRequestParam, TResponseParam>(string? requestPath) : IWebSocketService
	where TRequestParam : WebSocketServiceRequest
	where TResponseParam : WebSocketServiceResponse, new()
{
	////////////////////////////////////////////////
	// @静态常量
	////////////////////////////////////////////////

	#region 静态常量

	public const double WebSocketAuthenticationDeadlineSecondsDefault = 10.0;

	#endregion


	////////////////////////////////////////////////
	// @静态缓存
	////////////////////////////////////////////////

	#region 静态缓存

	private static readonly ConcurrentDictionary<Type, ServiceAdministratorApiAttribute?>
		_serviceAdministratorApiAttributeInWebSocketServiceTypes = new();

	/// <summary>
	/// 获取指定“WebSocket服务类型”上的“管理员Api”特性。
	/// 带有缓存，避免“鉴权”每执行一次，就执行一次反射。
	/// </summary>
	private static ServiceAdministratorApiAttribute? GetServiceAdministratorApiAttributeInWebSocketServiceType(
		Type webSocketServiceType)
	{
		return _serviceAdministratorApiAttributeInWebSocketServiceTypes.GetOrAdd(
			webSocketServiceType,
			(type) => type.GetCustomAttribute<ServiceAdministratorApiAttribute>());
	}

	#endregion


	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string? RequestPath => requestPath;

	public string RequestPathKey { get; set; } = BrServiceMiddleware.CreateRequestPathKeyWithRequestPath(requestPath);

	public BrServiceMiddleware BrServiceMiddleware { get; set; } = default!;

	public HttpContext HttpContext { get; set; } = default!;

	public WebSocket WebSocket { get; set; } = default!;

	private ConcurrentDictionary<string, string?>? _customHttpHeaders = null;

	public IReadOnlyDictionary<string, string?>? CustomHttpHeaders => _customHttpHeaders;

	private readonly AsyncLock _webSocketSendLock = new();

	private readonly CancellationTokenSource _webSocketSendCancellationTokenSource = new();

	private readonly AsyncLock _webSocketReceiveLock = new();

	private readonly CancellationTokenSource _webSocketReceiveCancellationTokenSource = new();

	private bool _isConnectionAuthenticationChecked = false;

	private readonly CancellationTokenSource _checkConnectionAuthenticationCancellationTokenSource = new();

	private readonly Lock _connectionCheckAuthenticationLock = new();

	private long _connectionFirstAuthenticationTimeUtcTicks = 0;

	/// <summary>
	/// 连接第一次完成鉴权的时间。
	/// 内部使用“UtcTicks”存储，保证跨线程（接收循环写入、鉴权截止时限检查任务读取）的原子读写。
	/// </summary>
	public DateTimeOffset? ConnectionFirstAuthenticationTime
	{
		get
		{
			var utcTicks = Interlocked.Read(ref _connectionFirstAuthenticationTimeUtcTicks);
			return utcTicks == 0 ? null : new DateTimeOffset(utcTicks, TimeSpan.Zero);
		}
		set
		{
			Interlocked.Exchange(ref _connectionFirstAuthenticationTimeUtcTicks, value.HasValue ? value.Value.UtcTicks : 0);
		}
	}

	private long _connectionLastAuthenticationTimeUtcTicks = 0;

	/// <summary>
	/// 连接最后一次完成鉴权的时间。
	/// 内部使用“UtcTicks”存储，保证跨线程（接收循环写入、鉴权截止时限检查任务读取）的原子读写。
	/// </summary>
	public DateTimeOffset? ConnectionLastAuthenticationTime
	{
		get
		{
			var utcTicks = Interlocked.Read(ref _connectionLastAuthenticationTimeUtcTicks);
			return utcTicks == 0 ? null : new DateTimeOffset(utcTicks, TimeSpan.Zero);
		}
		set
		{
			Interlocked.Exchange(ref _connectionLastAuthenticationTimeUtcTicks, value.HasValue ? value.Value.UtcTicks : 0);
		}
	}

	public DateTimeOffset LastRequestReceiveTime { get; set; }

	public DateTimeOffset LastPingTime { get; set; }

	public DateTimeOffset LastPongTime { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public void InitializeWithBrServiceMiddleware(BrServiceMiddleware bxServiceMiddleware, HttpContext httpContext, WebSocket webSocket)
	{
		BrServiceMiddleware = bxServiceMiddleware;
		HttpContext = httpContext;
		WebSocket = webSocket;
	}
	private async Task SendStringAsync(string responseString, bool isConnectionNeedEnable)
	{
		var responseStringBytes = responseString.ToUtf8Bytes();
		await AsyncLock.LockAsync(null, () => _webSocketSendLock, async (asyncLock) =>
		{
			if (isConnectionNeedEnable && !WebSocket.IsConnectionEnable())
			{
				// !!! 拿到发送锁后，连接已失效，则不再发送。 !!!
				return;
			}
			//
			await WebSocket.SendWithTimeoutAsync(responseStringBytes, WebSocketMessageType.Text, true,
				BrServiceMiddleware.WebServiceConfig.WebSocketSendTimeoutSeconds, _webSocketSendCancellationTokenSource.Token);
			//
		});
	}

	public async Task SendStringAsync(string responseString)
	{
		await SendStringAsync(responseString, false);
	}

	public async Task SendObjectAsync(object responseObject)
	{
		var responseObjectJsonString = responseObject.ToJsonString();
		{ }
		await SendStringAsync(responseObjectJsonString);
	}

	public async Task SendResponseAsync(TResponseParam response)
	{
		await SendObjectAsync(response);
	}

	public async Task TrySendStringAsync(string responseString)
	{
		await SendStringAsync(responseString, true);
	}

	public async Task TrySendObjectAsync(object responseObject)
	{
		var responseObjectJsonString = responseObject.ToJsonString();
		{ }
		await TrySendStringAsync(responseObjectJsonString);
	}

	public async Task TrySendResponseAsync(TResponseParam response)
	{
		await TrySendObjectAsync(response);
	}

	#endregion


	////////////////////////////////////////////////
	// @事件节点
	////////////////////////////////////////////////

	#region 事件节点

	protected virtual async Task<bool> DidAcceptWebSocketInHttpContextAsync(
		HttpContext httpContext, WebSocket webSocket, double webSocketAuthenticationDeadlineSeconds)
	{
		if (!_isConnectionAuthenticationChecked)
		{
			lock (_connectionCheckAuthenticationLock)
			{
				if (!_isConnectionAuthenticationChecked)
				{
					_isConnectionAuthenticationChecked = true;
					_ = Task.Run(async () =>
					{
						try
						{
							// !!!
							await Task.Delay(TimeSpan.FromSeconds(webSocketAuthenticationDeadlineSeconds),
								_checkConnectionAuthenticationCancellationTokenSource.Token);
							// !!!
							var connectionFirstAuthenticationTime = ConnectionFirstAuthenticationTime;
							if (connectionFirstAuthenticationTime != null)
							{
								// 已鉴权。
								return;
							}

							// 超时未鉴权，则中断连接。
							await DiscountWebSocketInHttpContextAsync(HttpContext, WebSocket, new()
							{
								WebSocketCloseStatus = WebSocketCloseStatus.ProtocolError,
								WebSocketCloseDescription = "当前连接未在指定时间内完成鉴权操作。"
							});
						}
						catch (OperationCanceledException)
						{
							// !!! 连接已完成处理，并取消了本次“鉴权截止时限检查”，属于正常流程。 !!!
						}
						catch (ObjectDisposedException)
						{
							// !!! 连接已被释放（其他流程先一步完成关闭），属于正常流程。 !!!
						}
						catch (WebSocketException)
						{
							// !!! 连接已中断（关闭时对端已断开），属于正常流程。 !!!
						}
						catch (Exception exception)
						{
							// !!! 本任务“后台运行”，必须自行兜底，避免异常被静默丢弃。 !!!
							BrServiceMiddleware?.ExceptionLogFile?.Logs(
								this, "WebSocket 连接鉴权截止时限检查失败，程序异常。", exception);
						}
					}, _checkConnectionAuthenticationCancellationTokenSource.Token);
				}
			}
		}
		return await Task.FromResult(true);
	}

	protected virtual async Task DidConnectionAvailableInBrServiceMiddlewareAsync(
		BrServiceMiddleware bxServiceMiddleware, HttpContext httpContext, WebSocket webSocket)
	{
		await Task.CompletedTask;
	}

	protected virtual async Task<WebSocketServiceSocketCloseInfo?> DidProcessWebSocketInBrServiceMiddlewareAsync(
		BrServiceMiddleware bxServiceMiddleware, HttpContext httpContext, WebSocket webSocket)
	{
		var bytesBuffer = new BytesBuffer();
		while (webSocket.IsConnectionEnable())
		{
			var requestParamsStringReceiveResult = await AsyncLock.LockAsync<AsyncLock, WebSocketReceiveDataResult>
				(null, () => _webSocketReceiveLock, async (asyncLock) =>
				{
					WebSocketReceiveDataResult? requestParamsStringReceiveResult = null;
					requestParamsStringReceiveResult = await webSocket.ReceiveStringAsync(
						bytesBuffer, _webSocketReceiveCancellationTokenSource.Token);
					// !!!
					LastRequestReceiveTime = DateTimeOffset.Now;
					// !!!
					return requestParamsStringReceiveResult;
				});
			if (requestParamsStringReceiveResult.Result?.CloseStatus is { })
			{
				break;
			}

			TRequestParam? requestParam = null;
			if (requestParamsStringReceiveResult.StringReceived is { } requestParamsString
				&& !string.IsNullOrWhiteSpace(requestParamsString))
			{
				try
				{
					requestParam = requestParamsString.ToObjectByJsonDeserialize<TRequestParam>();
				}
				catch
				{
					await SendObjectAsync(new Response()
					{
						Error = Error.InvalidRequest,
						ErrorDescription = "解析请求参数出错。"
					});
					//
					continue;
					//
				}
			}
			if (requestParam?.CustomHttpHeaders is { } customHttpHeaders)
			{
				_customHttpHeaders = customHttpHeaders;
			}


			////////////////////////////////////////////////
			// 1/2，默认鉴权操作。
			////////////////////////////////////////////////
			var connectionAuthenticationTime = DateTimeOffset.Now;
			{
				////////////////////////////////////////////////
				// 1/3，检查请求参数有效性。
				////////////////////////////////////////////////
				var requestValidCheckResponse = bxServiceMiddleware.RequestValidCheckerFilter
					.CheckRequestHeadersForWebSocketServiceInHttpContext(httpContext, this, requestParam);
				if (requestValidCheckResponse?.IsFailed == true)
				{
					// !!!
					await SendObjectAsync(requestValidCheckResponse);
					// !!!
					return new()
					{
						WebSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
						WebSocketCloseDescription = requestValidCheckResponse.ErrorDescription
					};
				}


				////////////////////////////////////////////////
				// 2/3，检查用户令牌信息。
				////////////////////////////////////////////////
				var tokenCheckResponse = await bxServiceMiddleware.TokenCheckerFilter
					.CheckRequestTokenForWebSocketServiceInHttpContextAsync(httpContext, this, requestParam);
				if (tokenCheckResponse?.IsFailed == true)
				{
					// !!!
					await SendObjectAsync(tokenCheckResponse);
					// !!!
					return new()
					{
						WebSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
						WebSocketCloseDescription = tokenCheckResponse.ErrorDescription
					};
				}


				////////////////////////////////////////////////
				// 3/3，尝试，检查管理员令牌信息。
				////////////////////////////////////////////////
				if (GetServiceAdministratorApiAttributeInWebSocketServiceType(GetType()) is { } adminApiAttribute)
				{
					// !!! 模拟“MVC管道”中，“IActionFilter”的执行过程。 !!!
					var actionExecutingContext = new ActionExecutingContext(
						new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
						[],
						new Dictionary<string, object?>(),
						this);
					// !!!
					adminApiAttribute.OnActionExecuting(actionExecutingContext);
					// !!!

					// !!! “OnActionExecuting”中，设置了“Result”，即：管理员令牌信息检查失败。 !!!
					if (actionExecutingContext.Result != null)
					{
						var adminApiCheckFailedResponse
							= (actionExecutingContext.Result as JsonResult)?.Value as Response
							?? new Response()
							{
								Error = Error.UserTokenNeedPermission,
								ErrorDescription = "管理员会话令牌校验失败。"
							};
						// !!!
						await SendObjectAsync(adminApiCheckFailedResponse);
						// !!!
						return new()
						{
							WebSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
							WebSocketCloseDescription = adminApiCheckFailedResponse.ErrorDescription
						};
					}
				}
			}
			if (ConnectionFirstAuthenticationTime == null)
			{
				var isConnectionFirstAuthentication = false;
				lock (_connectionCheckAuthenticationLock)
				{
					if (ConnectionFirstAuthenticationTime == null)
					{
						// !!!
						ConnectionFirstAuthenticationTime = connectionAuthenticationTime;
						isConnectionFirstAuthentication = true;
						// !!!
					}
				}
				if (isConnectionFirstAuthentication)
				{
					await DidConnectionAvailableInBrServiceMiddlewareAsync(
						bxServiceMiddleware, httpContext, webSocket);
				}
			}
			// !!!
			ConnectionLastAuthenticationTime = connectionAuthenticationTime;
			// !!!


			////////////////////////////////////////////////
			// 2/2，业务内容处理。
			////////////////////////////////////////////////
			WebSocketServiceSocketCloseInfo? webSocketCloseInfo = null;
			if (requestParam?.IsWebSocketPing == true)
			{
				//
				LastPingTime = requestParam.WebSocketPingTime;
				LastPongTime = LastRequestReceiveTime;
				//
				var responseInfo = await DidProcessPingRequestAsync(requestParam, LastRequestReceiveTime);
				if (responseInfo != null)
				{
					if (responseInfo.ResponseParam is { } responseParam)
					{
						await SendObjectAsync(responseParam);
					}
					webSocketCloseInfo = responseInfo.WebSocketCloseInfo;
				}
			}
			else
			{
				var responseInfo = await DidProcessRequestAsync(requestParam);
				if (responseInfo != null)
				{
					if (responseInfo.ResponseParam is { } responseParam)
					{
						await SendObjectAsync(responseParam);
					}
					webSocketCloseInfo = responseInfo.WebSocketCloseInfo;
				}
			}
			if (webSocketCloseInfo != null)
			{
				return webSocketCloseInfo;
			}
		}
		return null;
	}

	protected virtual async Task<WebSocketServiceResponseInfo<WebSocketServiceResponse>?> DidProcessPingRequestAsync(
		TRequestParam requestParam, DateTimeOffset poneDateTime)
	{
		return await Task.FromResult(new WebSocketServiceResponseInfo<WebSocketServiceResponse>()
		{
			ResponseParam = new()
			{
				IsWebSocketPong = true,
				WebSocketPongTime = poneDateTime
			}
		});
	}

	protected abstract Task<WebSocketServiceResponseInfo<TResponseParam>?> DidProcessRequestAsync(TRequestParam? requestParam);

	protected virtual async Task WillDiscountWebSocketInHttpContextAsync(
		HttpContext httpContext, WebSocket webSocket, WebSocketServiceSocketCloseInfo webSocketCloseInfo)
	{
		await Task.CompletedTask;
	}

	protected virtual async Task DidDiscountWebSocketInHttpContextAsync(
		HttpContext httpContext, WebSocket webSocket, WebSocketServiceSocketCloseInfo webSocketCloseInfo)
	{
		await Task.CompletedTask;
	}

	#endregion


	////////////////////////////////////////////////
	// @实现“WebSocketService”
	////////////////////////////////////////////////

	#region 实现“WebSocketService”

	public async Task<bool> AcceptWebSocketInHttpContextAsync(
		HttpContext httpContext, WebSocket webSocket, double? webSocketAuthenticationDeadlineSeconds)
	{
		if (webSocketAuthenticationDeadlineSeconds == null || webSocketAuthenticationDeadlineSeconds.Value <= 0)
		{
			webSocketAuthenticationDeadlineSeconds = WebSocketAuthenticationDeadlineSecondsDefault;
		}
		var isAccepted = await DidAcceptWebSocketInHttpContextAsync(
			httpContext, webSocket, webSocketAuthenticationDeadlineSeconds.Value);
		if (!isAccepted)
		{
			// !!! 连接未被接受，中间件不会调用“Process/Discount”，此处需停止“鉴权截止时限检查”。 !!!
			_checkConnectionAuthenticationCancellationTokenSource.Cancel();
		}
		return isAccepted;
	}

	public async Task<WebSocketServiceSocketCloseInfo?> ProcessWebSocketInBrServiceMiddlewareAsync(
		BrServiceMiddleware bxServiceMiddleware, HttpContext httpContext, WebSocket webSocket)
	{
		try
		{
			return await DidProcessWebSocketInBrServiceMiddlewareAsync(bxServiceMiddleware, httpContext, WebSocket);
		}
		catch (OperationCanceledException)
		{
			// !!!
			// 发送、接收的“取消令牌”，只会在“DiscountWebSocketInHttpContextAsync”中被取消，
			// 即：必然有其他流程（如“鉴权截止时限检查任务”）正在关闭当前连接，
			// 由该流程负责关闭，这里直接静默结束，避免被中间件记为“程序异常”。
			// !!!
			return null;
		}
		finally
		{
			// !!!
			_checkConnectionAuthenticationCancellationTokenSource.Cancel();
			// !!!
		}
	}

	public async Task DiscountWebSocketInHttpContextAsync(
		HttpContext httpContext, WebSocket webSocket, WebSocketServiceSocketCloseInfo webSocketCloseInfo)
	{
		_checkConnectionAuthenticationCancellationTokenSource.Cancel();
		await _webSocketSendCancellationTokenSource.CancelAsync();
		await _webSocketReceiveCancellationTokenSource.CancelAsync();

		await AsyncLock.LockAsync(null, () => _webSocketSendLock, async (asyncLock) =>
		{
			await AsyncLock.LockAsync(null, () => _webSocketReceiveLock, async (asyncLock) =>
			{
				// !!!
				await WillDiscountWebSocketInHttpContextAsync(httpContext, webSocket, webSocketCloseInfo);
				// !!!
				await webSocket.CloseWithTimeoutAsync(webSocketCloseInfo.WebSocketCloseStatus, webSocketCloseInfo.WebSocketCloseDescription,
					BrServiceMiddleware.WebServiceConfig.WebSocketCloseTimeoutSeconds, httpContext.RequestAborted);
				// !!!
				await DidDiscountWebSocketInHttpContextAsync(httpContext, webSocket, webSocketCloseInfo);
				// !!!
			});
		});
	}

	#endregion
}