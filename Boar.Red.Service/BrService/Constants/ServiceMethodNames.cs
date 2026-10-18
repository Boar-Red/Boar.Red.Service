namespace Boar.Red.Service.BrService.Constants
{
	public class ServiceMethodNames
	{
		////////////////////////////////////////////////
		// @静态常量
		////////////////////////////////////////////////

		#region 静态常量

		public const string ServiceShutdown = "/brService/shutdown";

		public const string Heartbeat = "/heartbeat/beat";

		public const string OpenConnection = "/brClientConnectionInfo/openConnection";

		public const string CloseConnection = "/brClientConnectionInfo/closeConnection";

		#endregion
	}
}
