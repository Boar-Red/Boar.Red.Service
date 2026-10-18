namespace Boar.Red.Service.BrService.ViewModels;

public class WebSocketServiceTypeIndexInfo(string? requstPath, string requstPathKey, Type webSocketServiceType)
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string? RequstPath { get; set; } = requstPath;

	public string RequestPathKey { get; set; } = requstPathKey;

	public Type WebSocketServiceType { get; set; } = webSocketServiceType;

	#endregion
}