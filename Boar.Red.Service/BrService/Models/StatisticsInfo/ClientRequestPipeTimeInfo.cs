namespace Boar.Red.Service.BrService.Models.StatisticsInfo
{
	/// <summary>
	/// 【客户端】，请求管道时间信息。
	/// </summary>
	public class ClientRequestPipeTimeInfo : RequestPipeTimeInfo
	{
		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性

		////////////////////////////////////////////////
		// @请求信息
		////////////////////////////////////////////////

		/// <summary>
		/// 开始，连接服务器的开始时间点，单位为：毫秒。
		/// </summary>
		public long StopwatchTime_0_ToConnectService { get; set; }

		/// <summary>
		/// 开始，反序列化请求字节的开始时间点，单位为：毫秒。
		/// </summary>
		public long StopwatchTime_1_ToSerializeRequestBytes { get; set; }

		/// <summary>
		/// 开始，将请求字节加入待发送队列的开始时间点，单位为：毫秒。
		/// </summary>
		public long StopwatchTime_2_ToAddRequestBytesToNeedSendQueue { get; set; }

		/// <summary>
		/// 开始，发送请求字节的开始时间点，单位为：毫秒。
		/// </summary>
		public long StopwatchTime_3_ToSendRequestBytesBegin { get; set; }

		/// <summary>
		/// 结束，发送请求字节的开始时间点，单位为：毫秒。
		/// </summary>
		public long StopwatchTime_4_ToSendRequestBytesEnd { get; set; }

		/// <summary>
		/// 开始，接收服务响应字节的时间点，单位为：毫秒。
		/// </summary>
		public long StopwatchTime_5_ToReceiveResponseBytesBegin { get; set; }

		/// <summary>
		/// 开始，将响应字节加入待处理队列的开始时间点，单位为：毫秒。
		/// </summary>
		public long StopwatchTime_6_ToAddResponseBytesToNeedProcessQueue { get; set; }

		/// <summary>
		/// 开始，反序列化响应对象的开始时间点，单位为：毫秒。
		/// </summary>
		public long StopwatchTime_7_ToDeserializeResponseObjectBegin { get; set; }

		/// <summary>
		/// 开始，创建响应对象的开始时间点，单位为：毫秒。
		/// </summary>
		public long StopwatchTime_8_ToDeserializeResponseObjectEnd { get; set; }

		#endregion


		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性

		public ClientRequestPipeTimeInfo(RequestPipeType type)
		    : base(type)
		{ }

		public void SetTime_0_ToConnectService(DateTime now)
		{
			_stopwatch.Start();
			State = RequestPipeState.Started;
			StartTime = now;
			StopwatchTime_0_ToConnectService = 0;
		}

		public void SetTime_1_ToSerializeRequestBytes()
		{
			StopwatchTime_1_ToSerializeRequestBytes = _stopwatch.ElapsedMilliseconds;
		}

		public void SetTime_2_ToAddRequestBytesToNeedSendQueue()
		{
			StopwatchTime_2_ToAddRequestBytesToNeedSendQueue = _stopwatch.ElapsedMilliseconds;
		}

		public void SetTime_3_ToSendRequestBytesBegin()
		{
			StopwatchTime_3_ToSendRequestBytesBegin = _stopwatch.ElapsedMilliseconds;
		}

		public void SetTime_4_ToSendRequestBytesEnd()
		{
			StopwatchTime_4_ToSendRequestBytesEnd = _stopwatch.ElapsedMilliseconds;
		}

		public void SetTime_5_ToReceiveResponseBytesBegin()
		{
			StopwatchTime_5_ToReceiveResponseBytesBegin = _stopwatch.ElapsedMilliseconds;
		}

		public void SetTime_6_ToAddResponseBytesToNeedProcessQueue(DateTime timeOfReceiveData)
		{
			StopwatchTime_6_ToAddResponseBytesToNeedProcessQueue
			    = (long)(timeOfReceiveData - StartTime).TotalMilliseconds;
			//= _stopwatch.ElapsedMilliseconds;
		}

		public void SetTime_7_ToDeserializeResponseObjectBegin()
		{
			StopwatchTime_7_ToDeserializeResponseObjectBegin = _stopwatch.ElapsedMilliseconds;
		}

		public void SetTime_8_ToDeserializeResponseObjectEnd(DateTime now)
		{
			_stopwatch.Stop();
			if (State == RequestPipeState.Started)
			{
				State = RequestPipeState.Success;
			}
			CompletedTime = now;
			StopwatchTime_8_ToDeserializeResponseObjectEnd = _stopwatch.ElapsedMilliseconds;
		}

		#endregion
	}
}
