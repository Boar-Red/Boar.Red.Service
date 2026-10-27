using Microsoft.AspNetCore.Mvc;

namespace Boar.Red.Service.MemoryTests;

public class ConstructionFailureController : Controller
{
	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ConstructionFailureController(ScopedProbe probe, AsyncScopedProbe asyncProbe)
	{
		throw new InvalidOperationException("Expected failure after both scoped dependencies have been resolved.");
	}

	#endregion
}
