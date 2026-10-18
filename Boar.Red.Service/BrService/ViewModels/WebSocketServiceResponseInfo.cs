namespace Boar.Red.Service.BrService.ViewModels;

public class WebSocketServiceResponseInfo<TResponseParam>
	: WebSocketServiceSocketCloseInfo
	where TResponseParam : class
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public TResponseParam? ResponseParam { get; set; }

	public WebSocketServiceSocketCloseInfo? WebSocketCloseInfo { get; set; }

	#endregion
}