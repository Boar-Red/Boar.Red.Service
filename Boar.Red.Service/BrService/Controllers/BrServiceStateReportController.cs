using BaoXia.Constants;
using BaoXia.Constants.Models;
using Boar.Red.Service.Attributes;
using Boar.Red.Service.BrService.Middlewares;
using Boar.Red.Service.BrService.Models.StatisticsInfo;
using Boar.Red.Service.BrService.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Boar.Red.Service.BrService.Controllers;

public class BrServiceStateReportController(BrServiceMiddleware bxServiceMiddleware) : Controller
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	protected BrServiceMiddleware _bxServiceMiddleware = bxServiceMiddleware;

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public class IndexRequest
	{
		public DateTime ReportBeginTime { get; set; }

		public DateTime ReportEndTime { get; set; }

		public bool IsValid
		{
			get
			{
				if (ReportEndTime >= ReportBeginTime)
				{
					return true;
				}
				return false;
			}
		}
	}

	public class IndexResponse : Response
	{
		public ServiceStateReportInfo? ServiceStateReportInfo { get; set; }
	}

	[HttpPost]
	[LanAccessible]
	public IActionResult Index([FromBody] IndexRequest request)
	{
		var response = new IndexResponse();
		try
		{
			if (request?.IsValid == true)
			{
				if (_bxServiceMiddleware != null)
				{
					var reportBeginTime = request.ReportBeginTime;
					var reportEndTime = request.ReportEndTime;
					var allRequestStatisticsInfesOrderedByRequestTimeAsc
					    = _bxServiceMiddleware.RequestStatisticsInfesOrderedByRequestTimeAsc;
					var requestStatisticsInfesOrderedByRequestTimeAsc
					    = new List<ServiceRequestStatisticsInfo>();
					var serviceConfig
					    = _bxServiceMiddleware.Config;
					var serviceNodeInfo
					    = _bxServiceMiddleware.ServiceNodeInfo;
					var serviceStateReportInfo = new ServiceStateReportInfo(
					    serviceNodeInfo.GroupName,
					    serviceNodeInfo.ServiceName,
					    serviceNodeInfo.NodeName,
					    serviceConfig.ServiceInfo?.ServiceDescription,
					    //
					    reportBeginTime,
					    reportEndTime,
					    //
					    _bxServiceMiddleware.State,
					    serviceNodeInfo.StartTime,
					    serviceNodeInfo.StartDuration,
					    DateTime.Now,
					    //
					    serviceNodeInfo.LoadRate,
					    requestStatisticsInfesOrderedByRequestTimeAsc); ;
					if (allRequestStatisticsInfesOrderedByRequestTimeAsc?.Length > 0)
					{
						foreach (var serviceRequestStatisticsInfo
						    in
						    allRequestStatisticsInfesOrderedByRequestTimeAsc)
						{
							var serviceRequestStatisticsInfoStatisticsInSecondTime
							    = serviceRequestStatisticsInfo.StatisticsInSecondTime;
							if (serviceRequestStatisticsInfoStatisticsInSecondTime.CompareTo(
							    reportBeginTime) >= 0
							&& serviceRequestStatisticsInfoStatisticsInSecondTime.CompareTo(
							    reportEndTime) <= 0)
							{
								requestStatisticsInfesOrderedByRequestTimeAsc.Add(
								    serviceRequestStatisticsInfo);
							}
						}
					}
					response.ServiceStateReportInfo = serviceStateReportInfo;
				}
				else
				{
					response.Error = Error.ObjectNotExisted;
				}
			}
			else
			{
				response.Error = Error.InvalidRequest;
			}
		}
		catch (Exception exception)
		{
			_bxServiceMiddleware.ExceptionLogFile?.Logs(this, "获取服务报告信息失败，程序异常。", exception);
			//
			response.Error = Error.ProgramError;
		}
		return Json(response);
	}

	#endregion
}