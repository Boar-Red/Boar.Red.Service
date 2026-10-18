using Boar.Red.Service.Constants;

namespace Boar.Red.Service.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute(ServiceLifeCycle lifeCycle = ServiceLifeCycle.Singleton) : Attribute
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ServiceLifeCycle LifeCycle { get; set; } = lifeCycle;

	#endregion
}
