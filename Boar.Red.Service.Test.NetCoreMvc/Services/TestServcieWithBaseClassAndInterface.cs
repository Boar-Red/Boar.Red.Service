using Boar.Red.Service.Attributes;
using Boar.Red.Service.Test.NetCoreMvc.Interfaces;
using Boar.Red.Service.Test.NetCoreMvc.Services.Base;

namespace Boar.Red.Service.Test.NetCoreMvc.Services;

[Service]
public class TestServcieWithBaseClassAndInterface
: BaseService, TestInterface
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public override string ServiceName { get; set; } = "TestServcieWithBaseClass";

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