using Boar.Red.Service.BrService.Models.StatisticsInfo;

namespace Boar.Red.Service.MemoryTests;

public static class StatisticsRetentionTests
{
	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static void Run()
	{
		var config = new MemoryTestConfig();
		var middleware = new MemoryTestMiddleware(config);
		var now = DateTime.Now;
		middleware.SeedStatistics(new(RequestPipeType.ApiInvokeRequest, DateTime.MinValue),
			new(RequestPipeType.ApiInvokeRequest, now.AddHours(-2)), new(RequestPipeType.ApiInvokeRequest, now.AddMinutes(-1)));
		middleware.Aggregate();
		Check(middleware.StatisticsCount == 1 && middleware.RequestStatisticsInfesOrderedByRequestTimeAsc.Length == 1,
			"Every expired item in the sorted prefix must be removed, including DateTime.MinValue.");
		var snapshot = middleware.RequestStatisticsInfesOrderedByRequestTimeAsc;
		middleware.Aggregate();
		Check(ReferenceEquals(snapshot, middleware.RequestStatisticsInfesOrderedByRequestTimeAsc),
			"An unchanged aggregation must not allocate another statistics snapshot.");
		middleware.SeedStatistics(new(RequestPipeType.ApiInvokeRequest, now.AddMinutes(-1)),
			new(RequestPipeType.ClientConnectionOpen, now.AddHours(-2)));
		middleware.Aggregate();
		Check(middleware.StatisticsCount == 1, "Late or out-of-order statistics must expire even when a newer record precedes them.");

		middleware.SeedStatistics(new(RequestPipeType.ApiInvokeRequest, now.AddHours(-3)), new(RequestPipeType.ApiInvokeRequest, now.AddHours(-2)));
		middleware.Aggregate();
		Check(middleware.StatisticsCount == 0, "The cleanup must also handle an entirely expired list.");
		middleware.Aggregate();
		Check(middleware.StatisticsCount == 0, "An empty aggregation must stay empty.");

		middleware.AddPipe(new(RequestPipeType.ApiInvokeRequest) { State = RequestPipeState.Success, StartTime = now, CompletedTime = now });
		middleware.Aggregate();
		var sameSecond = middleware.RequestStatisticsInfesOrderedByRequestTimeAsc.Single();
		Check(sameSecond.RequestsCount == 1 && sameSecond.ResponsesCount == 1,
			"A request completed in the same second must not be counted twice.");
		middleware.AddPipe(new(RequestPipeType.ClientConnectionOpen) { State = RequestPipeState.Success, StartTime = now, CompletedTime = DateTime.MinValue });
		middleware.Aggregate();
		Check(middleware.RequestStatisticsInfesOrderedByRequestTimeAsc.All(info => info.StatisticsInSecondTime > now.AddHours(-1)),
			"An unset completion time must not create a new expired statistics bucket.");

		middleware.SeedStatistics(Enumerable.Range(0, 10000).Select(index => new ServiceRequestStatisticsInfo(RequestPipeType.ApiInvokeRequest,
			now.AddHours(-4).AddSeconds(index))).ToArray());
		config.BrServiceConfig.RequestStatisticsInLiveSeconds = 0;
		middleware.Aggregate();
		Check(middleware.StatisticsCount == 0 && middleware.StatisticsCapacity == 0,
			"Disabling statistics must release retained records and their large backing array.");
		using var socket = new ProbeWebSocket();
		middleware.QueueBusinessRequest(socket);
		Check(middleware.PendingRequestCount == 1, "Business requests must still enter the processing queue when statistics are disabled.");
		Console.WriteLine("PASS partial/full/empty expiration, snapshot reuse, invalid times, accurate same-second counts and statistics disabled without dropping requests");
	}

	private static void Check(bool condition, string description)
	{
		if (!condition)
		{
			throw new InvalidOperationException(description);
		}
	}

	#endregion
}
