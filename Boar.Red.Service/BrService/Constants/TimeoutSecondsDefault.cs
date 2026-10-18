namespace Boar.Red.Service.BrService.Constants
{
	public class TimeoutSecondsDefault
	{

		////////////////////////////////////////////////
		// @静态常量
		////////////////////////////////////////////////

		#region 静态常量

		/// <summary>
		/// 应用程序登录超时秒数，默认为 10 秒，注意：使用服务发现功能时，服务发现功能的业务帧率通常是以秒计算的，因此如果设置登录超时秒数小于等于 1 秒数，会遇到服务尚未发现，登录就已经超时的情况。
		/// </summary>
		public const double ClientConnectionOpen = 10.0;

		#endregion
	}
}
