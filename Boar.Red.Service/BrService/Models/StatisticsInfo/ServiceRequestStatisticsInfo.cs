using BaoXia.Utils.Extensions;

namespace Boar.Red.Service.BrService.Models.StatisticsInfo;

public class ServiceRequestStatisticsInfo
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	/// <summary>
	/// 统计的请求类型。
	/// </summary>
	public RequestPipeType RequestType { get; set; }

	/// <summary>
	/// 统计时间，精度为：秒。
	/// </summary>
	public DateTime StatisticsInSecondTime { get; set; }

	////////////////////////////////////////////////
	// @次数信息
	////////////////////////////////////////////////

	/// <summary>
	/// 当前时间的请求数量。
	/// </summary>
	public int RequestsCount { get; set; }

	/// <summary>
	/// 当前时间的响应数量。
	/// </summary>
	public int ResponsesCount { get; set; }

	public bool IsValid
	{
		get
		{
			if (RequestsCount > 0
			    || ResponsesCount > 0)
			{
				return true;
			}
			return false;
		}
	}

	////////////////////////////////////////////////
	// @耗时信息
	////////////////////////////////////////////////

	/// <summary>
	/// 开始，将请求字节加入待处理队列的开始时间点，单位为：毫秒。
	/// </summary>
	public double Milliseconds_0_OfRequestBytesInNeedProcessQueue { get; set; }

	/// <summary>
	/// 开始，反序列化请求字节的开始时间点，单位为：毫秒。
	/// </summary>
	public double Milliseconds_1_OfDeserializeRequestBytes { get; set; }

	/// <summary>
	/// 开始，路由请求的开始时间点，单位为：毫秒。
	/// </summary>
	public double Milliseconds_2_OfRouteRequest { get; set; }

	/// <summary>
	/// 开始，创建响应对象的开始时间点，单位为：毫秒。
	/// </summary>
	public double Milliseconds_3_OfCreateResponseObject { get; set; }

	/// <summary>
	/// 开始，序列化响应对象的开始时间点，单位为：毫秒。
	/// </summary>
	public double Milliseconds_4_OfSerializeResponseObject { get; set; }

	/// <summary>
	/// 开始，将响应字节加入待发送队列的开始时间点，单位为：毫秒。
	/// </summary>
	public double Milliseconds_5_OfResponseBytesInNeedSendQueue { get; set; }

	/// <summary>
	/// 开始，发送响应字节的开始时间点，单位为：毫秒。
	/// </summary>
	public double Milliseconds_6_OfSendResponseBytes { get; set; }

	/// <summary>
	/// 处理请求的合计耗时，单位为：毫秒。
	/// </summary>
	public double Milliseconds_Total_OfProcessRequest
	{
		get
		{
			var totalMilliseconds
			    = Milliseconds_0_OfRequestBytesInNeedProcessQueue
			    + Milliseconds_1_OfDeserializeRequestBytes
			    + Milliseconds_2_OfRouteRequest
			    + Milliseconds_3_OfCreateResponseObject
			    + Milliseconds_4_OfSerializeResponseObject
			    + Milliseconds_5_OfResponseBytesInNeedSendQueue
			    + Milliseconds_6_OfSendResponseBytes;
			{ }
			return totalMilliseconds;
		}
	}

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ServiceRequestStatisticsInfo(
	    RequestPipeType requestType,
	    DateTime statisticsInSecondTime)
	{
		RequestType = requestType;

		StatisticsInSecondTime = statisticsInSecondTime;
	}

	public void AddRequest(ServiceRequestPipeTimeInfo? requestPipeTimeInfo)
	{
		if (requestPipeTimeInfo == null)
		{
			return;
		}


		////////////////////////////////////////////////
		// 类型不同，不叠加：
		////////////////////////////////////////////////
		if (RequestType != RequestPipeType.Unknown
		    && RequestType != requestPipeTimeInfo.Type)
		{
			return;
		}


		if (StatisticsInSecondTime.EqualsInSecond(requestPipeTimeInfo.StartTime))
		{
			RequestsCount++;
		}
		if (!StatisticsInSecondTime.EqualsInSecond(requestPipeTimeInfo.CompletedTime))
		{
			return;
		}

		ResponsesCount++;

		////////////////////////////////////////////////
		// 只有在当前秒响应完成的请求通道，才记录分析各个操作的耗时情况：
		////////////////////////////////////////////////

		Milliseconds_0_OfRequestBytesInNeedProcessQueue
		    = requestPipeTimeInfo.StopwatchTime_1_ToDeserializeRequestBytes
		    - requestPipeTimeInfo.StopwatchTime_0_ToAddRequestBytesToNeedProcessQueue;

		Milliseconds_1_OfDeserializeRequestBytes
		    = requestPipeTimeInfo.StopwatchTime_2_ToRouteRequest
		    - requestPipeTimeInfo.StopwatchTime_1_ToDeserializeRequestBytes;

		Milliseconds_2_OfRouteRequest
		    = requestPipeTimeInfo.StopwatchTime_3_ToCreateResponseObject
		    - requestPipeTimeInfo.StopwatchTime_2_ToRouteRequest;

		Milliseconds_3_OfCreateResponseObject
		    = requestPipeTimeInfo.StopwatchTime_4_ToSerializeResponseObject
		    - requestPipeTimeInfo.StopwatchTime_3_ToCreateResponseObject;

		Milliseconds_4_OfSerializeResponseObject
		    = requestPipeTimeInfo.StopwatchTime_5_ToAddResponseBytesToNeedSendQueue
		    - requestPipeTimeInfo.StopwatchTime_4_ToSerializeResponseObject;

		Milliseconds_5_OfResponseBytesInNeedSendQueue
		    = requestPipeTimeInfo.StopwatchTime_6_ToSendResponseBytesBegin
		    - requestPipeTimeInfo.StopwatchTime_5_ToAddResponseBytesToNeedSendQueue;

		Milliseconds_6_OfSendResponseBytes
		    = requestPipeTimeInfo.StopwatchTime_7_ToSendResponseBytesEnd
		    - requestPipeTimeInfo.StopwatchTime_6_ToSendResponseBytesBegin;
	}

	#endregion
}
