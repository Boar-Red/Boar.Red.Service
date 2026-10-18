using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils.Constants;
using BaoXia.Utils.Models;

namespace Boar.Red.Service.Extensions;

public static class StringsSafeCheckResultExtension
{
	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	extension(StringsSafeCheckResult? stringsSafeCheckResult)
	{
		public Result? ToUnsafeResult()
		{
			if (stringsSafeCheckResult == null)
			{
				return null;
			}

			switch (stringsSafeCheckResult.UnsafeType)
			{
				default:
				case StringUnsafeType.Unknown:
					{
						return null;
					}
				case StringUnsafeType.HackingBySqlInjection:
					{
						return new()
						{
							Error = Error.HackingBySqlInjection,
							ErrorDescription = stringsSafeCheckResult.UnsafeDescription
							?? $"“{stringsSafeCheckResult.UnsafeStringName}”字段含有危险的“Sql注入代码”。"
						};
					}
			}
		}

		public TResult? ToUnsafeResult<TResult>() where TResult : Result, new()
		{
			if (stringsSafeCheckResult == null)
			{
				return null;
			}

			switch (stringsSafeCheckResult.UnsafeType)
			{
				default:
				case StringUnsafeType.Unknown:
					{
						return null;
					}
				case StringUnsafeType.HackingBySqlInjection:
					{
						return new()
						{
							Error = Error.HackingBySqlInjection,
							ErrorDescription = stringsSafeCheckResult.UnsafeDescription
							?? $"“{stringsSafeCheckResult.UnsafeStringName}”字段含有危险的“Sql注入代码”。"
						};
					}
			}
		}
	}

	#endregion
}
