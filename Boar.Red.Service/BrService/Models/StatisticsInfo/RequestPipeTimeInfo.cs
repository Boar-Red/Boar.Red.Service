using System.Diagnostics;

namespace Boar.Red.Service.BrService.Models.StatisticsInfo
{

	public class RequestPipeTimeInfo
	{
		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性

		////////////////////////////////////////////////
		// @基本信息
		////////////////////////////////////////////////

		protected Stopwatch _stopwatch;

		/// <summary>
		/// 管道类型。
		/// </summary>
		public RequestPipeType Type { get; set; }

		/// <summary>
		/// 管道的状态。
		/// </summary>
		public RequestPipeState State { get; set; }

		/// <summary>
		/// 管道是否已结束。
		/// </summary>
		public bool IsCompleted
		{
			get
			{
				switch (State)
				{
					default:
					case RequestPipeState.Unknown:
					case RequestPipeState.Started:
						break;
					case RequestPipeState.Success:
					case RequestPipeState.Failed:
					case RequestPipeState.Exception:
					case RequestPipeState.Cancelled:
						{
							return true;
						}
				}
				return false;
			}
		}

		/// <summary>
		/// 统计信息的创建。
		/// </summary>
		public DateTime CreateTime { get; } = DateTime.Now;

		/// <summary>
		/// 管道开始时间（请求时间）。
		/// </summary>
		public DateTime StartTime { get; set; }

		/// <summary>
		/// 管道完成时间（响应完毕时间）。
		/// </summary>
		public DateTime CompletedTime { get; set; }

		#endregion


		////////////////////////////////////////////////
		// @自身实现
		////////////////////////////////////////////////

		#region 自身实现

		public RequestPipeTimeInfo(RequestPipeType type)
		{
			_stopwatch = new Stopwatch();

			Type = type;
		}

		#endregion
	}
}
