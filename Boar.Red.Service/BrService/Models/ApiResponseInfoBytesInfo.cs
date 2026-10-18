using System.Text;

namespace Boar.Red.Service.BrService.Models;

public class ApiResponseInfoBytesInfo(
    SocketInfo socketInfo,
    //
    byte[] bytes,
    Encoding textEncoding,
    //
    DateTime timeOfReceiveData)
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public SocketInfo SocketInfo { get; set; } = socketInfo;

	public byte[] Bytes { get; set; } = bytes;

	public System.Text.Encoding TextEncoding { get; set; } = textEncoding;

	public DateTime TimeOfReceiveData { get; set; } = timeOfReceiveData;

	#endregion
}
