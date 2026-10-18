namespace Boar.Red.Service.Models
{
	public class RouteAction : IDisposable
	{
		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性

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

		~RouteAction()
		{
			if (Controller is IDisposable disposableController)
			{
				disposableController.Dispose();
			}
		}

		#endregion


		////////////////////////////////////////////////
		// @实现“IDisposable”
		////////////////////////////////////////////////

		#region 实现“IDisposable”

		public void Dispose()
		{
			if (Controller is IDisposable disposableController)
			{
				disposableController.Dispose();
			}

			////////////////////////////////////////////////
			GC.SuppressFinalize(this);
			////////////////////////////////////////////////
		}

		#endregion
	}
}
