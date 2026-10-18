using BaoXia.Utils.Extensions;
using BaoXia.Utils.Models;
using System.Net.WebSockets;

namespace Boar.Red.Service.BrService.Models;

public class SocketInfo(
    WebSocket socket,
    ClientIpInfo clientIpInfo) : IDisposable
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public WebSocket Socket { get; set; } = socket;

	public ClientIpInfo ClientIpInfo { get; set; } = clientIpInfo;

	public string? ErrorToOpenConnection { get; set; }

	public string? ErrorDescriptionToOpenConnection { get; set; }

	/// <summary>
	/// 当前Socket连接是否可用。
	/// </summary>
	public bool IsConnectionEnable
	{
		get
		{
			if (Socket.IsConnectionEnable())
			{
				return true;
			}
			return false;
		}
	}

	public ApiRequestInfo<object>? HeartbeatApiRequestInfoNeedSend { get; set; }

	public ApiResponseInfoAbstract? HeartbeatApiResponseInfoNeedSend { get; set; }


	/// <summary>
	/// 最后一次收到业务数据的时间。
	/// </summary>
	public DateTime LastTimeOfReceiveBusinessData { get; set; }

	/// <summary>
	/// 最后一次发送业务数据的时间。
	/// </summary>
	public DateTime LastTimeOfSendBusinessData { get; set; }

	/// <summary>
	/// 最后一次接收或发送业务数据的时间。
	/// </summary>
	public DateTime LastTimeOfReceiveOrSendBusinessData
	{
		get
		{
			if (LastTimeOfSendBusinessData > LastTimeOfReceiveBusinessData)
			{
				return LastTimeOfSendBusinessData;
			}
			return LastTimeOfReceiveBusinessData;
		}
	}

	/// <summary>
	/// 最后一次收到心跳包的时间。
	/// </summary>
	public DateTime LastTimeOfReceiveHeartbeat { get; set; }

	/// <summary>
	/// 最后一次发送心跳包的时间。
	/// </summary>
	public DateTime LastTimeOfSendHeartbeat { get; set; }

	/// <summary>
	/// 最后一次接收或发送心跳包的时间。
	/// </summary>
	public DateTime LastTimeOfReceiveOrSendHeartbeat
	{
		get
		{
			if (LastTimeOfSendHeartbeat > LastTimeOfReceiveHeartbeat)
			{
				return LastTimeOfSendHeartbeat;
			}
			return LastTimeOfReceiveHeartbeat;
		}
	}

	/// <summary>
	/// 最后一次接收业务，或心跳数据的时间。
	/// </summary>
	public DateTime LastTimeOfReceiveData
	{
		get
		{
			if (LastTimeOfReceiveHeartbeat > LastTimeOfReceiveBusinessData)
			{
				return LastTimeOfReceiveHeartbeat;
			}
			return LastTimeOfReceiveBusinessData;
		}
	}

	/// <summary>
	/// 最后一次发送业务，或心跳数据的时间。
	/// </summary>
	public DateTime LastTimeOfSendData
	{
		get
		{
			if (LastTimeOfSendHeartbeat > LastTimeOfSendBusinessData)
			{
				return LastTimeOfSendHeartbeat;
			}
			return LastTimeOfSendBusinessData;
		}
	}

	/// <summary>
	/// 最后一次接收，或发送业务，或心跳数据的时间。
	/// </summary>
	public DateTime LastTimeOfReceiveOrSendData
	{
		get
		{
			var lastTimeOfReceiveData = LastTimeOfReceiveData;
			var lastTimeOfSendData = LastTimeOfSendData;
			if (lastTimeOfSendData > lastTimeOfReceiveData)
			{
				return lastTimeOfSendData;
			}
			return lastTimeOfReceiveData;
		}
	}

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	~SocketInfo()
	{
		if (Socket is ClientWebSocket clientWebSocket)
		{
			clientWebSocket.Dispose();
		}
	}

	public bool IsNeedSendHeartbeatAtTime(
	    DateTime time,
	    double heartbeatIntervalSeconds)
	{
		if ((time - LastTimeOfReceiveOrSendData).TotalSeconds
		    > heartbeatIntervalSeconds)
		{
			return true;
		}
		return false;
	}

	#endregion


	////////////////////////////////////////////////
	// @实现“IDisposable”
	////////////////////////////////////////////////

	#region 实现“IDisposable”

	public void Dispose()
	{
		if (Socket is ClientWebSocket clientWebSocket)
		{
			clientWebSocket.Dispose();
		}

		////////////////////////////////////////////////
		GC.SuppressFinalize(this);
		////////////////////////////////////////////////
	}


	#endregion


}

