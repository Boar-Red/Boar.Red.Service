namespace Boar.Red.Service.Constants;

public class HttpHeaderKeys
{
	////////////////////////////////////////////////
	// @ServiceAdministrator
	////////////////////////////////////////////////

	#region ServiceAdministrator

	public class ServiceAdministrator
	{
		////////////////////////////////////////////////
		// @静态常量
		////////////////////////////////////////////////

		#region 静态常量

		public const string SessionToken = "BrService-Administrator-Session-Token";

		public const string ClientIpAddress = "BrService-Administrator-Client-Ip-Address";

		#endregion
	}

	#endregion


	////////////////////////////////////////////////
	// @BrApiClientInfo
	////////////////////////////////////////////////

	#region BrApiClientInfo

	public class BrApiClientInfo
	{
		////////////////////////////////////////////////
		// @静态常量
		////////////////////////////////////////////////

		#region 静态常量


		public const string ServiceProviderIdentity = "Br-Api-Service-Provider-Identity";

		public const string ServiceProviderClientIdentity = "Br-Api-Service-Provider-Client-Identity";

		public const string Version = "Br-Api-Client-Version";

		public const string InstallChannel = "Br-Api-Client-Install-Channel";

		public const string OSName = "Br-Api-Client-OS-Name";

		public const string OSVersion = "Br-Api-Client-OS-Version";

		public const string DeviceName = "Br-Api-Client-Device-Name";

		public const string DeviceId = "Br-Api-Client-Device-Id";

		#endregion
	}

	#endregion
}
