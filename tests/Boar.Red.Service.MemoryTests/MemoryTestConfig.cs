using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.WebService.Models;
using BaoXia.Utils;

namespace Boar.Red.Service.MemoryTests;

public class MemoryTestConfig : ConfigFile
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public WebServiceConfig WebServiceConfig { get; set; } = new();

	public ServiceConfig BrServiceConfig { get; set; } = new() { RequestStatisticsInLiveSeconds = 3600 };

	#endregion
}
