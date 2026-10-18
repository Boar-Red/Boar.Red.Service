using BaoXia.Utils;
using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.Constants;
using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.BrService.Models.StatisticsInfo;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace Boar.Red.Service.BrService.Extensions;

public static class WebSocketDataPacketExtension
{
	////////////////////////////////////////////////
	// @类方法，数据包处理
	////////////////////////////////////////////////

	#region 类方法

	static async Task<bool> SendDataPacketAsync(this WebSocket webSocket, DataPacket? dataPacket,
	   System.Text.Encoding textEncoding, DataPackageEncryptionMethod encryptionMethod,
	   string encryptionAesKey, int dataPacketSizeToCompression, string? dataPacketCompressionMethodName,
	   CancellationToken cancellationToken)
	{
		if (dataPacket == null)
		{
			return false;
		}

		var dataPacketBytes = dataPacket.ToBytes(textEncoding, encryptionMethod,
		    encryptionAesKey, dataPacketSizeToCompression, dataPacketCompressionMethodName);
		{
			await webSocket.SendAsync(dataPacketBytes, dataPacket.WebSocketMessageType, true, cancellationToken);
		}
		return true;
	}


	public static async Task<DataPacket?> ReceiveDataPacketAsync(
		this WebSocket webSocket, BytesBuffer? receiveBuffer, System.Text.Encoding textEncoding,
		string encryptionAesKey, CancellationToken cancellationToken)
	{
		var bytesReceiveResult = await webSocket.ReceiveByteArrayAsync(receiveBuffer, cancellationToken);
		if (bytesReceiveResult == null)
		{
			return null;
		}

		var dataPacketReceived = DataPacket.CreateDataPacketWithBytes(bytesReceiveResult.BytesReceived, textEncoding, encryptionAesKey);
		{
			dataPacketReceived?.WebSocketReceiveResult = bytesReceiveResult.Result;
		}
		return dataPacketReceived;
	}

	public static async IAsyncEnumerable<DataPacket?> ReceiveDataPacketsAsync(
		this WebSocket webSocket, BytesBuffer? receiveBuffer, System.Text.Encoding textEncoding,
		string encryptionAesKey, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (receiveBuffer == null)
		{
			yield break;
		}

		DataPacket? dataPacket = null;
		while (dataPacket?.WebSocketReceiveResult?.CloseStatus == null)
		{
			dataPacket = await webSocket.ReceiveDataPacketAsync(receiveBuffer, textEncoding, encryptionAesKey, cancellationToken);
			// !!!
			yield return dataPacket;
			// !!!
		}
	}

	#endregion

	////////////////////////////////////////////////
	// @类方法，请求信息处理
	////////////////////////////////////////////////

	#region 类方法

	public static async Task<bool> SendApiRequestInfoAsync(this WebSocket webSocket, ApiRequestInfo<object>? apiRequestInfo,
		System.Text.Encoding textEncoding, DataPackageEncryptionMethod encryptionMethod,
		string encryptionAesKey, int dataPacketSizeToCompression, string? dataPacketCompressionMethodName,
		CancellationToken cancellationToken)
	{
		if (apiRequestInfo == null)
		{
			return false;
		}

		// ⏱⏱⏱，【计时埋点】
		apiRequestInfo.ClientRequestPipeTimeInfo?.SetTime_1_ToSerializeRequestBytes();
		// ⏱⏱⏱，【计时埋点】
		////////////////////////////////////////////////
		var dataPacket = apiRequestInfo.ToDataPacket(textEncoding, encryptionMethod, encryptionAesKey);
		////////////////////////////////////////////////
		if (apiRequestInfo.ClientRequestPipeTimeInfo is ClientRequestPipeTimeInfo clientRequestPipeTimeInfo)
		{
			// ⏱⏱⏱，【计时埋点】
			clientRequestPipeTimeInfo.SetTime_2_ToAddRequestBytesToNeedSendQueue();
			clientRequestPipeTimeInfo.SetTime_3_ToSendRequestBytesBegin();
			// ⏱⏱⏱，【计时埋点】
		}

		await SendDataPacketAsync(
		    webSocket,
		    dataPacket,
		    //
		    textEncoding,
		    encryptionMethod,
		    encryptionAesKey,
		    dataPacketSizeToCompression,
		    dataPacketCompressionMethodName,
		    //
		    cancellationToken);

		// ⏱⏱⏱，【计时埋点】
		apiRequestInfo.ClientRequestPipeTimeInfo?.SetTime_4_ToSendRequestBytesEnd();
		// ⏱⏱⏱，【计时埋点】

		return true;
	}

	public static async Task<ApiRequestInfo<MethodParamsType>?> ReceiveApiRequestInfoAsync<MethodParamsType>(
		this WebSocket webSocket, BytesBuffer? receiveBuffer, System.Text.Encoding textEncoding, string encryptionAesKey,
		RequestPipeType requestPipeTimeInfoType, CancellationToken cancellationToken)
	{
		var dataPacket = await webSocket.ReceiveDataPacketAsync(receiveBuffer, textEncoding, encryptionAesKey, cancellationToken);
		if (dataPacket == null)
		{
			return null;
		}
		// ⏱⏱⏱，【计时埋点】
		var now = DateTime.Now;
		var requestPipeTimeInfo = new ServiceRequestPipeTimeInfo(requestPipeTimeInfoType);
		requestPipeTimeInfo.SetTime_0_ToAddRequestBytesToNeedProcessQueue(now);
		requestPipeTimeInfo.SetTime_1_ToDeserializeRequestBytes();
		// ⏱⏱⏱，【计时埋点】
		var apiRequestInfo = dataPacket?.ToApiRequestInfo<MethodParamsType>();
		//apiRequestInfo.
		apiRequestInfo?.ServiceRequestPipeTimeInfo = requestPipeTimeInfo;
		return apiRequestInfo;
	}

	public static async IAsyncEnumerable<ApiRequestInfo<MethodParamsType>?> ReceiveApiRequestInfesAsync<MethodParamsType>(
		this WebSocket webSocket, BytesBuffer? receiveBuffer, System.Text.Encoding textEncoding, string encryptionAesKey,
		RequestPipeType requestPipeTimeInfoType, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (receiveBuffer == null)
		{
			yield break;
		}

		ApiRequestInfo<MethodParamsType>? apiRequestInfo = null;
		while (apiRequestInfo?.WebSocketReceiveResult?.CloseStatus == null)
		{
			apiRequestInfo
			    = await webSocket.ReceiveApiRequestInfoAsync<MethodParamsType>(
			    receiveBuffer,
			    textEncoding,
			    encryptionAesKey,
			    requestPipeTimeInfoType,
			    cancellationToken);
			// !!!
			yield return apiRequestInfo;
			// !!!
		}
	}

	#endregion


	////////////////////////////////////////////////
	// @类方法，响应信息处理
	////////////////////////////////////////////////

	#region 类方法

	public static async Task<bool> SendApiResponseInfoAsync(
	    this WebSocket webSocket, ApiResponseInfoAbstract? apiResponseInfo, System.Text.Encoding textEncoding,
	    DataPackageEncryptionMethod encryptionMethod, string encryptionAesKey, int dataPacketSizeToCompression,
	    string? dataPacketCompressionMethodName, CancellationToken cancellationToken)
	{
		if (apiResponseInfo == null)
		{
			return false;
		}

		// ⏱⏱⏱，【计时埋点】
		apiResponseInfo.ServiceRequestPipeTimeInfo?.SetTime_4_ToSerializeResponseObject();
		// ⏱⏱⏱，【计时埋点】
		var dataPacket = apiResponseInfo.ToDataPacket(
		    textEncoding,
		    encryptionMethod,
		    encryptionAesKey);

		if (apiResponseInfo.ServiceRequestPipeTimeInfo is ServiceRequestPipeTimeInfo serviceRequestPipeTimeInfo)
		{
			// ⏱⏱⏱，【计时埋点】
			serviceRequestPipeTimeInfo.SetTime_5_ToAddResponseBytesToNeedSendQueue();
			// ⏱⏱⏱，【计时埋点】
			{
				// 加入响应字节到处理队列的操作。
			}
			// ⏱⏱⏱，【计时埋点】
			serviceRequestPipeTimeInfo.SetTime_6_ToSendResponseBytesBegin();
			// ⏱⏱⏱，【计时埋点】
		}

		await SendDataPacketAsync(
		    webSocket,
		    dataPacket,
		    //
		    textEncoding,
		    encryptionMethod,
		    encryptionAesKey,
		    dataPacketSizeToCompression,
		    dataPacketCompressionMethodName,
		    //
		    cancellationToken);

		// ⏱⏱⏱，【计时埋点】
		apiResponseInfo
		    .ServiceRequestPipeTimeInfo
		    ?.SetTime_7_ToSendResponseBytesEnd(DateTime.Now);
		// ⏱⏱⏱，【计时埋点】

		return true;
	}

	public static async Task<ApiResponseInfo<MethodResultType>?> ReceiveApiResponseInfoAsync<MethodResultType>(
	    this WebSocket webSocket, BytesBuffer? receiveBuffer, System.Text.Encoding textEncoding, string encryptionAesKey,
	    ClientRequestPipeTimeInfo clientRequestPipeTimeInfo, CancellationToken cancellationToken)
	{
		// ⏱⏱⏱，【计时埋点】
		clientRequestPipeTimeInfo.SetTime_5_ToReceiveResponseBytesBegin();
		// ⏱⏱⏱，【计时埋点】
		var dataPacket = await webSocket.ReceiveDataPacketAsync(receiveBuffer, textEncoding, encryptionAesKey, cancellationToken);
		if (dataPacket == null)
		{
			return null;
		}
		// ⏱⏱⏱，【计时埋点】
		clientRequestPipeTimeInfo.SetTime_6_ToAddResponseBytesToNeedProcessQueue(DateTime.Now);
		{
			// 加入响应字节待处理队列的操作。
		}
		clientRequestPipeTimeInfo.SetTime_7_ToDeserializeResponseObjectBegin();
		// ⏱⏱⏱，【计时埋点】
		////////////////////////////////////////////////
		var apiResponseInfo
		    = dataPacket?.ToApiResponseInfo<MethodResultType>();
		////////////////////////////////////////////////
		// ⏱⏱⏱，【计时埋点】
		clientRequestPipeTimeInfo.SetTime_8_ToDeserializeResponseObjectEnd(DateTime.Now);
		// ⏱⏱⏱，【计时埋点】

		if (apiResponseInfo == null)
		{
			return null;
		}
		return apiResponseInfo;
	}

	public static async IAsyncEnumerable<ApiResponseInfo<MethodResultType>?> ReceiveApiResponseInfesAsync<MethodResultType>(
		this WebSocket webSocket, BytesBuffer? receiveBuffer, System.Text.Encoding textEncoding, string encryptionAesKey,
		RequestPipeType requestPipeType, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (receiveBuffer == null)
		{
			yield break;
		}

		ApiResponseInfo<MethodResultType>? apiResponseInfo = null;
		while (apiResponseInfo?.WebSocketReceiveResult?.CloseStatus == null)
		{
			var apiRequestPipeTimeInfo
			    = new ClientRequestPipeTimeInfo(requestPipeType);
			apiResponseInfo
			    = await webSocket.ReceiveApiResponseInfoAsync<MethodResultType>(
			    receiveBuffer,
			    //
			    textEncoding,
			    encryptionAesKey,
			    //
			    apiRequestPipeTimeInfo,
			    //
			    cancellationToken);
			// !!!
			yield return apiResponseInfo;
			// !!!
		}
	}

	#endregion
}