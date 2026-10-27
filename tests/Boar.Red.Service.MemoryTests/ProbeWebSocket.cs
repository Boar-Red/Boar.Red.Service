using System.Net.WebSockets;

namespace Boar.Red.Service.MemoryTests;

public class ProbeWebSocket : WebSocket
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	private readonly TaskCompletionSource _blockedOperation = new(TaskCreationOptions.RunContinuationsAsynchronously);

	private WebSocketState _state = WebSocketState.Open;

	public bool BlockSend { get; set; }

	public bool BlockClose { get; set; }

	public Action? BeforeSend { get; set; }

	public int AbortCount { get; private set; }

	public override WebSocketCloseStatus? CloseStatus => null;

	public override string? CloseStatusDescription => null;

	public override string? SubProtocol => null;

	public override WebSocketState State => _state;

	#endregion


	////////////////////////////////////////////////
	// @重载
	////////////////////////////////////////////////

	#region 重载

	public override void Abort()
	{
		AbortCount++;
		_state = WebSocketState.Aborted;
		_blockedOperation.TrySetCanceled();
	}

	public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
	{
		BeforeSend?.Invoke();
		return BlockSend ? _blockedOperation.Task : Task.CompletedTask;
	}

	public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
	{
		if (BlockClose)
		{
			return _blockedOperation.Task;
		}
		_state = WebSocketState.Closed;
		return Task.CompletedTask;
	}

	public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
	{
		return CloseAsync(closeStatus, statusDescription, cancellationToken);
	}

	public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
	{
		throw new NotSupportedException();
	}

	public override void Dispose()
	{
		_state = WebSocketState.Closed;
		_blockedOperation.TrySetCanceled();
	}

	#endregion
}
