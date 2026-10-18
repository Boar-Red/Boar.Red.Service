namespace Boar.Red.Service.BrService.Models;

public class CsrfTokenManagerConfig
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public double CsrfTokenLiveSecondsMax { get; set; }

	public int CsrfTokenLength { get; set; }

	public double? TokenCleanIntervalSeconds { get; set; }

	#endregion
}