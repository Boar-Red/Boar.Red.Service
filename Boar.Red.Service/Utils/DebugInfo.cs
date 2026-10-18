using BaoXia.Utils.Extensions;

namespace Boar.Red.Service.Utils;

public class DebugInfo
{
	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

#pragma warning disable CA2211 // 非常量字段应当不可见

	public static bool IsConsoleWriteEnable = true;

	public static bool IsSystemDiagnosticsDebugEnable = false;

	public static string[]? ValidDebugInfoTags =
	    [
	    "关机测试",
	"心跳包测试"
	    ];

#pragma warning restore CA2211 // 非常量字段应当不可见
	#endregion


	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static void WriteLine(
	    string[]? tags,
	    string? debugInfo)
	{
#if DEBUG
		string? finalDebugInfo = null;
		var isAnyValidTagMatched = true;
		if (tags?.Length > 0)
		{
			isAnyValidTagMatched = false;
			foreach (var tag in tags)
			{
				if (ValidDebugInfoTags?.Contains(tag) == true)
				{
					isAnyValidTagMatched = true;
				}

				var finalTag
				    = !tag.StartsWith("#")
				    ? "#" + tag
				    : tag;
				if (!finalTag.EndsWith("#"))
				{
					finalTag += "#";
				}

				if (string.IsNullOrEmpty(finalDebugInfo))
				{
					finalDebugInfo = finalTag;
				}
				else
				{
					finalDebugInfo += " " + finalTag;
				}
			}
		}
		if (isAnyValidTagMatched != true)
		{
			return;
		}

		if (!string.IsNullOrEmpty(debugInfo))
		{
			finalDebugInfo += "\t" + debugInfo;
		}

		if (IsConsoleWriteEnable)
		{
			Console.WriteLine(finalDebugInfo);
		}

		if (IsSystemDiagnosticsDebugEnable)
		{
			System.Diagnostics.Debug.WriteLine(finalDebugInfo);
		}
#endif
	}

	public static void WriteLine(
	    string? tag,
	    string? debugInfo)
	{
		WriteLine(
		    string.IsNullOrEmpty(tag)
		    ? null
		    : [tag],
		    debugInfo);
	}

	public static void WriteLine(
	    params string[] tagsAndDebugInfo)
	{
		if (tagsAndDebugInfo.Length < 1)
		{
			return;
		}

		var lastParamIndex = tagsAndDebugInfo.Length - 1;
		var tags = tagsAndDebugInfo.ArrayByRemoveAt(lastParamIndex);
		var debugInfo = tagsAndDebugInfo[lastParamIndex];

		WriteLine(
		    tags,
		    debugInfo);
	}

	#endregion



}