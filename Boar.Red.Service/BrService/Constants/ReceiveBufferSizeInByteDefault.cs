namespace Boar.Red.Service.BrService.Constants
{
	public class ReceiveBufferSizeInByteDefault
	{
		////////////////////////////////////////////////
		// @静态常量
		////////////////////////////////////////////////

		#region 静态常量

		/// <summary>
		/// 接收Api请求信息的默认缓存大小：256 KB。
		/// </summary>
		public const int ReceiveApplicationRequest = 256 * 1024;

		/// <summary>
		/// 接收Api响应信息的默认缓存大小：512 KB。
		/// </summary>
		public const int ReceiveServiceResponse = 512 * 1024;

		#endregion
	}
}
