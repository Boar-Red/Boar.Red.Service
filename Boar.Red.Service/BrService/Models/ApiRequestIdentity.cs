namespace Boar.Red.Service.BrService.Models
{
	public class ApiRequestIdentity
	{
		////////////////////////////////////////////////
		// @静态变量
		////////////////////////////////////////////////

		#region 静态变量

		private static long _requestIdentityMaxValue = 0;

		#endregion


		////////////////////////////////////////////////
		// @类方法
		////////////////////////////////////////////////

		#region 类方法

		public static string New()
		{
			// N: 6d80e9a97a1d468b80963dbf03766d8d
			// D: b3af9ddf - 91f8 - 4675 - 994c - a46365772ea8
			// B: { 3ef17527 - b953 - 4780 - a482 - bf6d73478842}
			// P: (44124fa9 - 27e5 - 480f - b5c8 - 46a48a475c4b)
			// X: { 0x1c4030f9,0x9877,0x4866,{ 0xb6,0x02,0xe0,0xaa,0xd7,0xcb,0xe9,0xf5} }
			// var guid = Guid.NewGuid();
			// var requestIdentity = guid.ToString("N");
			var requestIdentity = Interlocked.Increment(ref _requestIdentityMaxValue);
			{ }
			return requestIdentity.ToString();
		}

		public class Service
		{
			public const string Shutdown = "service/shutdown";

			public const string Heartbeat = "service/heartbeat";
		}

		public class Client
		{
			public const string CloseConnection = "client/closeConnection";

			public const string Heartbeat = "client/heartbeat";
		}

		#endregion
	}
}
