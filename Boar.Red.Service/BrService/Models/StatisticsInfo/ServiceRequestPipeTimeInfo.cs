namespace Boar.Red.Service.BrService.Models.StatisticsInfo;

/// <summary>
/// 【服务端】，请求管道时间信息。
/// </summary>

public class ServiceRequestPipeTimeInfo : RequestPipeTimeInfo
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	////////////////////////////////////////////////
	// @请求信息
	////////////////////////////////////////////////

	/// <summary>
	/// 开始，将请求字节加入待处理队列的开始时间点，单位为：毫秒。
	/// </summary>
	public long StopwatchTime_0_ToAddRequestBytesToNeedProcessQueue { get; set; }

	/// <summary>
	/// 开始，反序列化请求字节的开始时间点，单位为：毫秒。
	/// </summary>
	public long StopwatchTime_1_ToDeserializeRequestBytes { get; set; }

	/// <summary>
	/// 开始，路由请求的开始时间点，单位为：毫秒。
	/// </summary>
	public long StopwatchTime_2_ToRouteRequest { get; set; }

	/// <summary>
	/// 开始，创建响应对象的开始时间点，单位为：毫秒。
	/// </summary>
	public long StopwatchTime_3_ToCreateResponseObject { get; set; }

	/// <summary>
	/// 开始，序列化响应对象的开始时间点，单位为：毫秒。
	/// </summary>
	public long StopwatchTime_4_ToSerializeResponseObject { get; set; }

	/// <summary>
	/// 开始，将响应字节加入待发送队列的开始时间点，单位为：毫秒。
	/// </summary>
	public long StopwatchTime_5_ToAddResponseBytesToNeedSendQueue { get; set; }

	/// <summary>
	/// 开始，发送响应字节的开始时间点，单位为：毫秒。
	/// </summary>
	public long StopwatchTime_6_ToSendResponseBytesBegin { get; set; }

	/// <summary>
	/// 结束，发送响应字节的开始时间点，单位为：毫秒。
	/// </summary>
	public long StopwatchTime_7_ToSendResponseBytesEnd { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ServiceRequestPipeTimeInfo(RequestPipeType type)
	    : base(type)
	{ }

	public void SetTime_0_ToAddRequestBytesToNeedProcessQueue(
	    DateTime now)
	{
		_stopwatch.Start();
		State = RequestPipeState.Started;
		StartTime = now;
		StopwatchTime_0_ToAddRequestBytesToNeedProcessQueue = 0;
	}

	public void SetTime_1_ToDeserializeRequestBytes()
	{
		StopwatchTime_1_ToDeserializeRequestBytes = _stopwatch.ElapsedMilliseconds;
	}

	public void SetTime_2_ToRouteRequest()
	{
		StopwatchTime_2_ToRouteRequest = _stopwatch.ElapsedMilliseconds;
	}

	public void SetTime_3_ToCreateResponseObject()
	{
		StopwatchTime_3_ToCreateResponseObject = _stopwatch.ElapsedMilliseconds;
	}

	public void SetTime_4_ToSerializeResponseObject()
	{
		StopwatchTime_4_ToSerializeResponseObject = _stopwatch.ElapsedMilliseconds;
	}

	public void SetTime_5_ToAddResponseBytesToNeedSendQueue()
	{
		StopwatchTime_5_ToAddResponseBytesToNeedSendQueue = _stopwatch.ElapsedMilliseconds;
	}

	public void SetTime_6_ToSendResponseBytesBegin()
	{
		StopwatchTime_6_ToSendResponseBytesBegin = _stopwatch.ElapsedMilliseconds;
	}

	public void SetTime_7_ToSendResponseBytesEnd(DateTime now)
	{
		_stopwatch.Stop();
		if (State == RequestPipeState.Started)
		{
			State = RequestPipeState.Success;
		}
		CompletedTime = now;
		StopwatchTime_7_ToSendResponseBytesEnd = _stopwatch.ElapsedMilliseconds;
	}

	#endregion
}
