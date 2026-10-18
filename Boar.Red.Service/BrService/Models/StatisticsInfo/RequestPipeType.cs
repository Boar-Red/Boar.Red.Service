namespace Boar.Red.Service.BrService.Models.StatisticsInfo
{
	public enum RequestPipeType
	{
		/// <summary>
		/// 未知类型的请求通道。
		/// </summary>
		Unknown,

		/// <summary>
		/// 客户端登录请求。
		/// </summary>
		ClientConnectionOpen,

		/// <summary>
		/// Api调用请求。
		/// </summary>
		ApiInvokeRequest,

		/// <summary>
		/// 心跳包请求，来自客户端。
		/// </summary>
		HeartbeatRequestFromCient,

		/// <summary>
		/// 心跳包请求，来自服务端。
		/// </summary>
		HeartbeatRequestFromServer
	}
}

