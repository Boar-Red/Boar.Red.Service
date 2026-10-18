using Boar.Red.Service.Attributes;
using Boar.Red.Service.Test.NetCoreMvc.Interfaces;

namespace Boar.Red.Service.Test.NetCoreMvc.Services;

[Service]
public class TestServcieWithBaseInterface : TestInterface
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string ServiceName { get; } = "TestServcieWithBaseInterface";

	#endregion

	////////////////////////////////////////////////
	// @实现“TestInterface01”
	////////////////////////////////////////////////

	#region 实现“TestInterface01”


	public string GetInterfaceName()
	{
		return ServiceName;
	}

	#endregion


}