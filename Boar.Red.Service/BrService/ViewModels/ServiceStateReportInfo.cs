using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.Constants;
using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.BrService.Models.StatisticsInfo;

namespace Boar.Red.Service.BrService.ViewModels
{
	public class ServiceStateReportInfo(
		string? groupName,
		string? serviceName,
		string? nodeName,
		string? serviceDescription,
		//
		DateTime beginTime,
		DateTime endTime,
		//
		ServiceState serviceState,
		DateTime startTime,
		TimeSpan startDuration,
		DateTime systemNowTime,
		//
		double loadRate,
		IEnumerable<ServiceRequestStatisticsInfo>? serviceRequestStatisticsInfes = null) : ServiceBaseInfo(groupName,
		  serviceName,
		  nodeName,
		  serviceDescription)
	{
		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性


		/// <summary>
		/// 信息的起始时间。
		/// </summary>
		public DateTime BeginTime { get; set; } = beginTime;

		/// <summary>
		/// 信息的结束时间。
		/// </summary>
		public DateTime EndTime { get; set; } = endTime;

		/// <summary>
		/// 服务更新时间标题。
		/// </summary>
		public string ServiceUpdateTimeTitle
		{
			get
			{
				return this.ServiceUpdateTime.TitleOfDetailPageDefault();
			}
		}

		/// <summary>
		/// 服务状态。
		/// </summary>
		public ServiceState ServiceState { get; set; } = serviceState;

		/// <summary>
		/// 服务状态名称。
		/// </summary>
		public string? ServiceStateName { get; set; } = serviceState.Name();

		/// <summary>
		/// 服务开始时间。
		/// </summary>
		public DateTime StartTime { get; set; } = startTime;

		/// <summary>
		/// 服务开始时间标题。
		/// </summary>
		public string StartTimeTitle
		{
			get
			{
				return StartTime.TitleOfDetailPageDefault();
			}
		}

		/// <summary>
		/// 服务运行时长。
		/// </summary>
		public TimeSpan StartDuration { get; set; } = startDuration;

		/// <summary>
		/// 服务运行时长标题。
		/// </summary>
		public string StartDurationTitle
		{
			get
			{
				return (DateTime.Now - StartTime).TitleOfDetailPageDefault();
			}
		}

		/// <summary>
		/// 当前服务所在系统的当前时间信息。
		/// </summary>
		public DateTime SystemNowTime { get; set; } = systemNowTime;

		/// <summary>
		/// 负载比例。
		/// </summary>
		public double LoadRate { get; set; } = loadRate;

		/// <summary>
		/// 当前。
		/// </summary>
		public IEnumerable<ServiceRequestStatisticsInfo>? ServiceRequestStatisticsInfes { get; set; } = serviceRequestStatisticsInfes;

		#endregion
	}
}
