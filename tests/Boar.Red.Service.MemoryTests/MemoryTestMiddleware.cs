using Boar.Red.Service.BrService.Middlewares;
using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.BrService.Models.StatisticsInfo;
using Boar.Red.Service.Models;
using BaoXia.Utils.Models;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Boar.Red.Service.MemoryTests;

public class MemoryTestMiddleware : BrServiceMiddleware
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public int StatisticsCount => _requestStatisticsInfesOrderedByRequestTimeAsc.Count;

	public int StatisticsCapacity => _requestStatisticsInfesOrderedByRequestTimeAsc.Capacity;

	public int PendingRequestCount => _requestInfoBytesInfesNeedProcessed.Count;

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public MemoryTestMiddleware(MemoryTestConfig config) : base(true, null, config, nameof(MemoryTestConfig.WebServiceConfig),
		config, nameof(MemoryTestConfig.BrServiceConfig), null, null, null, null, null, null, null, null, null, null, null)
	{
	}

	public RouteAction? Resolve(string path) => GetRouteActionWithRequestUriPath(path);

	public Task<ApiResponseInfoAbstract?> InvokeRequestAsync(string path, WebSocket socket, RouteAction? suppliedRoute = null)
	{
		return InvokeApiWithApiRequestInfoAsync(new ApiRequestInfo<JsonElement>
		{
			RequestIdentity = Guid.NewGuid().ToString(), MethodName = path, MethodParams = JsonSerializer.SerializeToElement(new { })
		}, null, socket, CancellationToken.None, suppliedRoute);
	}

	public void SeedStatistics(params ServiceRequestStatisticsInfo[] statistics)
	{
		_requestStatisticsInfesOrderedByRequestTimeAsc.Clear();
		_requestStatisticsInfesOrderedByRequestTimeAsc.AddRange(statistics);
		RequestStatisticsInfesOrderedByRequestTimeAsc = statistics.ToArray();
	}

	public void AddPipe(ServiceRequestPipeTimeInfo pipe) => _requestPipeTimeInfes.Enqueue(pipe);

	public void Aggregate() => DidPolymerizeRequestPipeTimeInfes();

	public void QueueBusinessRequest(WebSocket socket)
	{
		AddRequestInfoBytesToProcessed(new SocketInfo(socket, new ClientIpInfo()), [1, 2, 3],
			new ClientConnectionInfo(1, new ApplicationInfo()), Encoding.UTF8,
			new ServiceRequestPipeTimeInfo(RequestPipeType.ApiInvokeRequest), DateTime.Now);
	}

	#endregion
}
