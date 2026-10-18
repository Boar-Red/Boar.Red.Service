using Microsoft.Extensions.Primitives;

namespace Boar.Red.Service.Extensions;

public static class StringValuesExtension
{
	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法


	public static string? FirstStringValue(this StringValues stringValues)
	{
		if (stringValues.Count <= 0)
		{
			return null;
		}
		return stringValues[0];
	}

	public static bool FirstBoolValue(this StringValues stringValues)
	{
		return bool.TryParse(
		    stringValues.FirstStringValue(),
		    out var result) && result;
	}

	public static int FirstIntValue(this StringValues stringValues)
	{
		return int.TryParse(
		    stringValues.FirstStringValue(),
		    out var result)
		    ? result
		    : 0;
	}

	public static long FirstLongValue(this StringValues stringValues)
	{
		return long.TryParse(
		    stringValues.FirstStringValue(),
		    out var result)
		    ? result
		    : 0;
	}

	public static float FirstFloatValue(this StringValues stringValues)
	{
		return float.TryParse(
		    stringValues.FirstStringValue(),
		    out var result)
		    ? result
		    : 0;
	}

	public static double FirstDoubleValue(this StringValues stringValues)
	{
		return double.TryParse(
		    stringValues.FirstStringValue(),
		    out var result)
		    ? result
		    : 0;
	}

	public static DateTime FirstDateTimeValue(this StringValues stringValues)
	{
		return DateTime.TryParse(
		    stringValues.FirstStringValue(),
		    out var result)
		    ? result
		    : DateTime.MinValue;
	}

	#endregion
}
