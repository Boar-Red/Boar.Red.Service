namespace Boar.Red.Service.BrService.Models;

public class ServiceDiscovererConfig
{

	////////////////////////////////////////////////
	// @静态常量
	////////////////////////////////////////////////

	#region 静态常量

	public const bool IsDisableDefault = false;

	public const int ListenPortDefault = 6000;

	public const double BroadcastIntervalSecondsDefault = 1.0;

	public const double ServiceFindTimeoutSecondsDefault = 10.0;

	public const int MaxTasksCountToSendDataPacketDefault = 8;
	public const int MaxTasksCountToUpdateServiceNodeInfesFromServiceManagerDefault = 8;

	#endregion


	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public bool IsDisable { get; set; } = IsDisableDefault;

	public int ListenPort { get; set; } = ListenPortDefault;

	public double BroadcastIntervalSeconds { get; set; } = BroadcastIntervalSecondsDefault;

	public double ServiceFindTimeoutSeconds { get; set; } = ServiceFindTimeoutSecondsDefault;

	public int MaxTasksCountToSendDataPacket { get; set; } = MaxTasksCountToSendDataPacketDefault;

	public int MaxTasksCountToUpdateServiceNodeInfesFromServiceManager { get; set; } = MaxTasksCountToUpdateServiceNodeInfesFromServiceManagerDefault;

	public double IntervalSecondsToUpdateServiceFoundInfes { get; set; }
	    = ServiceInfesManager.IntervalSecondsToUpdateServiceFoundInfesDefault;


	//public bool IsMissingSecondsToSetServiceNodeInfesStateUnknowEnable { get; set; }
	//	= ServiceInfesManager.IsMissingSecondsToSetServiceNodeInfesStateUnknowEnableDefault;

	public double MissingSecondsToSetServiceNodeInfesStateUnknow { get; set; }
	    = ServiceInfesManager.MissingSecondsToSetServiceNodeInfesStateUnknowDefault;

	public string? ServiceManagerAddress { get; set; }
	public int ServiceManagerDiscoveryListenPort { get; set; } = ListenPortDefault;

	#endregion
}
