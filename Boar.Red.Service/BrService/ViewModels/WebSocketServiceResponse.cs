using BaoXia.Constants.Models;

namespace Boar.Red.Service.BrService.ViewModels;

public class WebSocketServiceResponse : Response
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public bool IsWebSocketPong { get; set; }

	public DateTimeOffset WebSocketPongTime { get; set; }

	#endregion
}
