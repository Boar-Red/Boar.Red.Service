namespace Boar.Red.Service.BrService.Attributes;

/// <summary>
/// 非服务控制器标记特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class NotBrServiceControllerAttribute : Attribute
{
	public const string Description = "这不是一个长连接服务控制器";
}
