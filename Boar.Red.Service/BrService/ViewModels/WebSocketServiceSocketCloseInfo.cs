using System.Net.WebSockets;

namespace Boar.Red.Service.BrService.ViewModels;

public class WebSocketServiceSocketCloseInfo
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public WebSocketCloseStatus WebSocketCloseStatus { get; set; }

	public string? WebSocketCloseDescription { get; set; }

	#endregion
}