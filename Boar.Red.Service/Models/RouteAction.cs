using Microsoft.Extensions.DependencyInjection;

namespace Boar.Red.Service.Models
{
	public class RouteAction : IDisposable, IAsyncDisposable
	{
		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性

		private readonly IServiceScope? _serviceScope;

		private int _isDisposed;

		public RouteInfo RouteInfo { get; set; }

		public object Controller { get; set; }

		public string ActionName => RouteInfo.ActionName;

		public Dictionary<string, string>? ActionParams => RouteInfo.ActionParams;

		public int ActionParamsCountWithoutReservedParams => RouteInfo.ActionParamsCountWithoutReservedParams;

		public bool IsValid
		{
			get
			{
				if (ActionName.Length > 0)
				{
					return true;
				}
				return false;
			}
		}

		#endregion


		////////////////////////////////////////////////
		// @自身实现
		////////////////////////////////////////////////

		#region 自身实现

		public RouteAction(
		    RouteInfo routeInfo,
		    object controller)
		{
			RouteInfo = routeInfo;
			Controller = controller;
		}

		public RouteAction(RouteInfo routeInfo, object controller, IServiceScope serviceScope)
			: this(routeInfo, controller)
		{
			_serviceScope = serviceScope;
		}

		#endregion


		////////////////////////////////////////////////
		// @实现“IDisposable”
		////////////////////////////////////////////////

		#region 实现“IDisposable”

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
			{
				return;
			}
			if (_serviceScope != null)
			{
				// 控制器由作用域管理，不能再单独释放一次。
				_serviceScope.Dispose();
			}
			else if (Controller is IDisposable disposableController)
			{
				disposableController.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		public async ValueTask DisposeAsync()
		{
			if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
			{
				return;
			}
			if (_serviceScope is IAsyncDisposable asyncScope)
			{
				await asyncScope.DisposeAsync();
			}
			else if (_serviceScope != null)
			{
				_serviceScope.Dispose();
			}
			else if (Controller is IAsyncDisposable asyncController)
			{
				await asyncController.DisposeAsync();
			}
			else if (Controller is IDisposable disposableController)
			{
				disposableController.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
