using Boar.Red.Service.Attributes;
using Boar.Red.Service.Test.NetCoreMvc.Services.Base;

namespace Boar.Red.Service.Test.NetCoreMvc.Services;

[Service]
public class TestServcieWithBaseClass : BaseService
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public override string ServiceName { get; set; } = "TestServcieWithBaseClass";

	#endregion
}