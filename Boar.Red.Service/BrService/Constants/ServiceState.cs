namespace Boar.Red.Service.BrService.Constants;

public enum ServiceState
{
	/// <summary>
	/// 未知、离线状态。
	/// </summary>
	Offline = 0,

	/// <summary>
	/// 正在启动。
	/// </summary>
	InBooting = 1,

	/// <summary>
	/// 正在服务。
	/// </summary>
	InService = 2,

	/// <summary>
	/// 正在关机。
	/// </summary>
	InShuttingDown = 3,

	/// <summary>
	/// 已关机。
	/// </summary>
	InShutdown = 4
}
