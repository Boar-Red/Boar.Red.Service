namespace Boar.Red.Service.MemoryTests;

public class ScopedProbe : IDisposable
{
	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

	public static int Created;

	public static int Disposed;

	public static readonly List<WeakReference> References = [];

	#endregion


	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public bool IsDisposed { get; private set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ScopedProbe()
	{
		Created++;
		References.Add(new WeakReference(this));
	}

	#endregion


	////////////////////////////////////////////////
	// @实现“IDisposable”
	////////////////////////////////////////////////

	#region 实现“IDisposable”

	public void Dispose()
	{
		IsDisposed = true;
		Disposed++;
	}

	#endregion
}
