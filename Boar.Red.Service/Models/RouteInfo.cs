using BaoXia.Utils;
using BaoXia.Utils.Extensions;

namespace Boar.Red.Service.Models;

/// <summary>
/// 路由信息。
/// </summary>
public class RouteInfo(string controllerName, string actionName, Dictionary<string, string>? actionParams, int reservedActionParamsCount) : object
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string ControllerName { get; set; } = controllerName;

	public string ActionName { get; set; } = actionName;

	public string ActionPath
	{
		get
		{
			return ControllerName + Path.AltDirectorySeparatorChar + ActionName;
		}
	}

	public Dictionary<string, string>? ActionParams { get; set; } = actionParams;

	public int ReservedActionParamsCount { get; set; } = reservedActionParamsCount;

	public int ActionParamsCountWithoutReservedParams
	{
		get
		{
			var actionParams = ActionParams;
			if (actionParams != null)
			{
				return actionParams.Count - ReservedActionParamsCount;
			}
			return 0;
		}
	}

	public bool IsValid
	{
		get
		{
			if (ControllerName.Length > 0
			    && ActionName.Length > 0)
			{
				return true;
			}
			return false;
		}
	}

	#endregion


	////////////////////////////////////////////////
	// @实现“Object”
	////////////////////////////////////////////////

	#region 实现“Object”

	public override bool Equals(object? obj)
	{
		if (obj is not RouteInfo anotherRouteInfo)
		{
			return false;
		}

		if (StringUtil.EqualsStrings(ControllerName, anotherRouteInfo.ControllerName) != true)
		{
			return false;
		}

		if (StringUtil.EqualsStrings(
		    ActionName,
		    anotherRouteInfo.ActionName)
		     != true)
		{
			return false;
		}

		if (ActionParams?.IsEquals(anotherRouteInfo.ActionParams)
		    != true)
		{
			return false;
		}

		return true;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	#endregion
}
