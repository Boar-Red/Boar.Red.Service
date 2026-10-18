using Boar.Red.Service.BrService.Models.StatisticsInfo;
using System.Text;

namespace Boar.Red.Service.BrService.Models
{
	public class ApiRequestInfoBytesInfo
	{

		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性

		public ClientConnectionInfo ClientConnectionInfo { get; set; }

		public SocketInfo SocketInfo { get; set; }

		public byte[] Bytes { get; set; }

		public System.Text.Encoding TextEncoding { get; set; }

		public ServiceRequestPipeTimeInfo ServiceRequestPipeTimeInfo { get; set; }

		public DateTime TimeOfReceiveData { get; set; }

		#endregion


		////////////////////////////////////////////////
		// @自身实现
		////////////////////////////////////////////////

		#region 自身实现

		public ApiRequestInfoBytesInfo(
		    ClientConnectionInfo clientConnectionInfo,
		    //
		    SocketInfo socketInfo,
		    byte[] bytes,
		    Encoding textEncoding,
		    //
		    ServiceRequestPipeTimeInfo requestPipeTimeInfo,
		    DateTime timeOfReceiveData)
		{
			ClientConnectionInfo = clientConnectionInfo;

			SocketInfo = socketInfo;
			Bytes = bytes;
			TextEncoding = textEncoding;

			ServiceRequestPipeTimeInfo = requestPipeTimeInfo;
			TimeOfReceiveData = timeOfReceiveData;
		}

		#endregion
	}
}
