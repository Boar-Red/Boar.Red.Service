using System.Collections.Concurrent;

namespace Boar.Red.Service.BrService.Models.StatisticsInfo
{
	public class RequestPipeTimeInfes
	{

		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性

		protected ConcurrentQueue<ServiceRequestPipeTimeInfo> _requestPipeTimeInfes = new();

		#endregion


		////////////////////////////////////////////////
		// @自身实现
		////////////////////////////////////////////////

		#region 自身实现

		public void Add(ServiceRequestPipeTimeInfo? requestPipeTimeInfo)
		{
			if (requestPipeTimeInfo == null)
			{
				return;
			}

			_requestPipeTimeInfes.Enqueue(requestPipeTimeInfo);
		}

		#endregion
	}
}
