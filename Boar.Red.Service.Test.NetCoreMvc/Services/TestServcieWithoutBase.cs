using Boar.Red.Service.Attributes;

namespace Boar.Red.Service.Test.NetCoreMvc.Services;

[Service]
public class TestServcieWithoutBase
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string ServiceName { get; } = "TestServcieWithoutBase";

	#endregion
}