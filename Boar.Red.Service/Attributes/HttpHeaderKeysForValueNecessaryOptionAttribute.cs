using Boar.Red.Service.Constants;

namespace Boar.Red.Service.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class HttpHeaderKeysForValueNecessaryOptionAttribute : Attribute
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public HttpHeaderKeysForValueNecessaryCheckType HttpHeaderKeysCheckType { get; set; } = HttpHeaderKeysForValueNecessaryCheckType.NeedCheck;

	public string[]? HttpHeaderKeysNeedCheck { get; set; }

	public string[]? HttpHeaderKeysNotNeedCheck { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public HttpHeaderKeysForValueNecessaryOptionAttribute()
	{ }

	public HttpHeaderKeysForValueNecessaryOptionAttribute(
	    HttpHeaderKeysForValueNecessaryCheckType httpHeaderKeysCheckType,
	    string[]? httpHeaderKeysNeedCheck = null,
	    string[]? httpHeaderKeysNotNeedCheck = null)
	{
		HttpHeaderKeysCheckType = httpHeaderKeysCheckType;
		HttpHeaderKeysNeedCheck = httpHeaderKeysNeedCheck;
		HttpHeaderKeysNotNeedCheck = httpHeaderKeysNotNeedCheck;
	}

	#endregion
}
