using Microsoft.AspNetCore.Mvc;

namespace Boar.Red.Service.MemoryTests;

public class MemoryProbeController(ScopedProbe probe, AsyncScopedProbe asyncProbe) : Controller
{
	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

	public static int Disposed;

	public static readonly List<WeakReference> References = [];

	#endregion


	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	private readonly ScopedProbe _probe = probe;

	private readonly AsyncScopedProbe _asyncProbe = asyncProbe;

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public async Task<IActionResult> Success()
	{
		References.Add(new WeakReference(this));
		await Task.Yield();
		if (_probe.IsDisposed || _asyncProbe.IsDisposed)
		{
			throw new InvalidOperationException("Dependencies must remain active during an asynchronous controller action.");
		}
		return Json(new { IsActive = true });
	}

	public IActionResult Failure() => throw new InvalidOperationException("Expected controller failure.");

	public IActionResult RedirectOnce() => Redirect("/memoryProbe/success");

	public IActionResult RedirectCycle() => Redirect("/memoryProbe/redirectCycle");

	#endregion


	////////////////////////////////////////////////
	// @重载
	////////////////////////////////////////////////

	#region 重载

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Disposed++;
		}
		base.Dispose(disposing);
	}

	#endregion
}
