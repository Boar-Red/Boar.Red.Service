namespace Boar.Red.Service.BrService.Constants
{
	public enum ClientState
	{
		/// <summary>
		/// 正在启动。
		/// </summary>
		Unknown,

		/// <summary>
		/// 正在连接。
		/// </summary>
		ServiceConnecting,

		/// <summary>
		/// 已连接。
		/// </summary>
		ServiceConnected,

		/// <summary>
		/// 正在关机。
		/// </summary>
		Closing,

		/// <summary>
		/// 已关机。
		/// </summary>
		Closed
	}
}
