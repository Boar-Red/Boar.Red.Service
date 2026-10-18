using BaoXia.Constants.Models;
using System.Collections.Concurrent;

namespace Boar.Red.Service.BrService.ViewModels;

public class WebSocketServiceRequest : Request
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ConcurrentDictionary<string, string?>? CustomHttpHeaders { get; set; }

	public bool IsWebSocketPing { get; set; }

	public DateTimeOffset WebSocketPingTime { get; set; }

	#endregion
}
