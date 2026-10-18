namespace Boar.Red.Service.BrService.Constants
{
	/// <summary>
	/// 连接终端的描述信息。
	/// </summary>
	public class SocketCloseStatusDescription
	{
		////////////////////////////////////////////////
		// @静态常量
		////////////////////////////////////////////////

		#region 静态常量

		public const string ServiceInfo_EndpointUnavailable_ServiceShutdown = "服务信息，服务已关机。";
		public const string ServiceInfo_EndpointUnavailable_UnexpectedCloseOperation = "服务信息，意外的连接关闭操作。";

		public const string ClientInfo_NormalClosure_CloseConnection = "客户端信息，连接正常关闭。";
		public const string ClientInfo_EndpointUnavailable_UnexpectedCloseOperation = "客户端信息，意外的连接关闭操作。";

		public const string ApplicationInfo_PolicyViolation_RequestParseWithJsonFailed = "请求信息，使用Json格式解析失败。";
		public const string ApplicationInfo_PolicyViolation_ResponseParseWithJsonFailed = "请求信息，使用Json格式解析失败。";

		public const string ApplicationInfo_PolicyViolation_ConnectTimeout = "应用程序信息，连接服务超时。";
		public const string ApplicationInfo_PolicyViolation_ConnectFailed = "应用程序信息，连接服务失败。";
		public const string ApplicationInfo_PolicyViolation_NeedConnectFirst = "应用程序信息，需要在使用服务前完成登录。";
		public const string ApplicationInfo_PolicyViolation_ConnectFailedByProgramError = "应用程序信息，登录失败，程序错误。";

		public const string ApplicationInfo_PolicyViolation_TooManyConnections = "连接失败，连接数已达最大值。";
		public const string ApplicationInfo_PolicyViolation_NeedServiceUri = "调用服务失败，需要服务的Uri。";
		public const string ApplicationInfo_PolicyViolation_NeedRequestIdentity = "调用服务失败，需要请求Id。";
		public const string ApplicationInfo_PolicyViolation_NeedApplicationIdentity = "调用服务失败，需要应用程序Id。";
		public const string ApplicationInfo_PolicyViolation_NeedMethodName = "调用服务失败，需要请求方法名称。";
		public const string ApplicationInfo_PolicyViolation_MethodNotFound = "调用服务失败，没有找到要调用的方法。";

		public const string ApplicationInfo_InternalServerError_PrepareLoginRequestFailed = "调用服务失败，准备登录请求时发生错误。";

		#endregion
	}
}
