using Boar.Red.Service.BrService.Middlewares;
using Boar.Red.Service.BrService.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;

namespace Boar.Red.Service.BrService.Interfaces;

public interface IWebSocketService
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string? RequestPath { get; }

	public string RequestPathKey { get; set; }

	public BrServiceMiddleware BrServiceMiddleware { get; set; }

	public HttpContext HttpContext { get; set; }

	public WebSocket WebSocket { get; set; }

	public IReadOnlyDictionary<string, string?>? CustomHttpHeaders { get; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public void InitializeWithBrServiceMiddleware(BrServiceMiddleware bxServiceMiddleware, HttpContext httpContext, WebSocket webSocket);

	public Task<bool> AcceptWebSocketInHttpContextAsync(
		HttpContext httpContext, WebSocket webSocket, double? webSocketAuthenticationDeadlineSeconds);

	public Task<WebSocketServiceSocketCloseInfo?> ProcessWebSocketInBrServiceMiddlewareAsync(
		BrServiceMiddleware bxServiceMiddleware, HttpContext httpContext, WebSocket webSocket);

	public Task DiscountWebSocketInHttpContextAsync(
		HttpContext httpContext, WebSocket webSocket, WebSocketServiceSocketCloseInfo webSocketCloseInfo);

	#endregion
}