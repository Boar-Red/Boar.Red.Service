using BaoXia.Utils;
using System.Net;
using System.Text.Json.Serialization;

namespace Boar.Red.Service.BrService.ViewModels;

public abstract class UdpDataPacket
{

	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	[JsonIgnore]
	public IPEndPoint? SendObjectIPEndPoint { get; set; }

	[JsonIgnore]
	public abstract string DataPacketName { get; }


	public DateTime DataPacketSendTime { get; set; }


	public DateTime DataPacketReceiveTime { get; set; }

	public TimeSpan DataPacketTransportTimeSpan
	{
		get
		{
			return DataPacketReceiveTime - DataPacketSendTime;
		}
	}

	[JsonIgnore]
	public IPAddress? UdpEndPointIPAddress { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static UdpDataPacketInstanceType? CreateUdpDataPacketWithBytes<UdpDataPacketInstanceType>(
	    ref BytesOperator bytesOperator,
	    bool isUdpDataPacketNameHadRead)
	    where UdpDataPacketInstanceType : UdpDataPacket, new()
	{
		// 1/2，读取，数据包名称等基础信息：
		if (isUdpDataPacketNameHadRead == false)
		{
			var udpDataPacketName = bytesOperator.ReadString();
			if (string.IsNullOrEmpty(udpDataPacketName))
			{
				return null;
			}
		}

		// 2/2，读取，实例数据内容：
		var udpDataPacketSendTime = bytesOperator.ReadDateTime();
		var udpDataPacketInstance = new UdpDataPacketInstanceType()
		{
			DataPacketSendTime = udpDataPacketSendTime
		};
		{
			udpDataPacketInstance.ReadDataBytes(ref bytesOperator);
		}

		return udpDataPacketInstance;
	}

	public static byte[] CreateBytesWithUdpDataPacket(
	    UdpDataPacket? udpDataPacketInstance,
	    out BytesOperator bytesOperator)
	{
		bytesOperator = new BytesOperator();
		if (udpDataPacketInstance == null)
		{
			return Array.Empty<byte>();
		}

		// 1/2，写入，数据包名称等基础信息：
		bytesOperator.Write(udpDataPacketInstance.DataPacketName);
		bytesOperator.Write(udpDataPacketInstance.DataPacketSendTime);

		// 2/2，写入，实例数据内容：
		{
			udpDataPacketInstance.WriteDataBytes(ref bytesOperator);
		}

		var udpDataPacketInstanceBytes = bytesOperator.ToBytes();
		{ }
		return udpDataPacketInstanceBytes;
	}

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	protected void WriteDataBytes(ref BytesOperator bytesOperator)
	{
		this.DidWriteDataBytes(ref bytesOperator);
	}

	protected void ReadDataBytes(ref BytesOperator bytesOperator)
	{
		this.DidReadDataBytes(ref bytesOperator);
	}

	#endregion


	////////////////////////////////////////////////
	// @事件节点
	////////////////////////////////////////////////

	#region 事件节点

	protected abstract void DidWriteDataBytes(ref BytesOperator bytesOperator);
	protected abstract void DidReadDataBytes(ref BytesOperator bytesOperator);

	#endregion
}