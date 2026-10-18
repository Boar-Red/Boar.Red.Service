namespace Boar.Red.Service.BrService.Constants
{
	/// <summary>
	/// 命名规范。
	/// </summary>
	public class NamingConventions
	{
		////////////////////////////////////////////////
		// @静态常量
		////////////////////////////////////////////////

		#region 静态常量

		public class DataPacketHeaderName
		{
			public const string RequestIdentity = "Request-Identity";

			public const string EncryptionMethod = "Encryption-Method";

			public const string EncryptionNonce = "Encryption-Nonce";

			public const string MethodName = "Method-Name";

		}

		public class DataPacketSectionNames
		{
			public const string ApiRequestInfo = "ApiRequestInfo";
			public const string ApiRequestInfo_MethodParams = "ApiRequestInfo.MethodParams";

			public const string ApiResponseInfo = "ApiResponseInfo";
			public const string ApiResponseInfo_MethodParams = "ApiResponseInfo.MethodParams";
			public const string ApiResponseInfo_FileInfo = "ApiResponseInfo.FileInfo";
		}

		public const string ControllerNameSuffix = "Controller";

		#endregion
	}
}
