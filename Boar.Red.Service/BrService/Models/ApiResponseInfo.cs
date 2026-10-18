using BaoXia.Utils;
using System.Text.Json.Serialization;

namespace Boar.Red.Service.BrService.Models;

public class ApiResponseInfo<ResultClass> : ApiResponseInfoAbstract
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	/// <summary>
	/// 方法调用结果参数。
	/// </summary>
	[JsonIgnore]
	public ResultClass? Result { get; set; }

	/// <summary>
	/// 方法调用结果参数类型。
	/// </summary>
	[JsonIgnore]
	public override Type ResultTypeInfo
	{
		get
		{
			return typeof(ResultClass);
		}
	}

	/// <summary>
	/// 方法调用结果参数，“object”类型。
	/// </summary>
	[JsonIgnore]
	public override object? ResultInObject
	{
		get
		{
			return Result;
		}
		set
		{
			if (value is ResultClass methodResult)
			{
				Result = methodResult;
			}
		}
	}


	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ApiResponseInfo()
	{ }

	public ApiResponseInfo(string requestIdentity)
	    : base(requestIdentity)
	{ }

	public override string ToString()
	{
		if (StatusCode == null)
		{
			return $"Api请求未开始，没有有效的响应状态码。";
		}
		return $"{StatusCode}，{StringUtil.FirstNotEmpty(StatusDescription?.TrimEnd('。', '.'), "无")}。";
	}

	public string ToStringEndWithoutFullStop()
	{
		return ToString().Trim('.', '。');
	}

	public string ToStringWithResult()
	{
		var stringValue = ToString();
		if (Result is BaoXia.Constants.Models.Result bxResult)
		{
			stringValue += bxResult.ToString();
		}
		return stringValue;
	}

	#endregion
}