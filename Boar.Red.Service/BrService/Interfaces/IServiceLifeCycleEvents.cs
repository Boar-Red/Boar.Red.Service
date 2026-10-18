namespace Boar.Red.Service.BrService.Interfaces;

public interface IServiceLifeCycleEvents
{
	////////////////////////////////////////////////
	// @事件节点
	////////////////////////////////////////////////

	#region 事件节点

	/// <summary>
	/// 服务启动顺序编号，可选值为：
	/// 小于”0“时，当前服务：不会阻塞其他服务的启动，不会阻塞应用程序的启动； 
	/// 等于“0”时，当前服务：不会阻塞其他服务的启动，“会”阻塞应用程序的启动；  
	/// 大于“0”时，当前服务：会阻塞其他自动顺序编号比当前服务大的服务的启动，“会”阻塞应用程序的启动； 
	/// </summary>
	public abstract int ServiceStartupSequenceNumber { get; }

	public abstract Task DidServiceBootingAsync(
	    Action<double> toReceiveBootingProgress,
	    CancellationToken cancellationToken);

	public abstract Task DidServiceStartedAsync(
	    CancellationToken cancellationToken);

	public abstract Task DidServiceShutdownAsync(
	    Action<double> toReceiveShutdownProgress,
	    CancellationToken cancellationToken);

	public abstract void DidApplicationNormalExit(
	    CancellationToken cancellationToken);

	#endregion
}