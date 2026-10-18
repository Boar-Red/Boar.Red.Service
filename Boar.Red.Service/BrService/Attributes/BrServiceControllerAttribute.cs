namespace Boar.Red.Service.BrService.Attributes;

/// <summary>
/// 服务控制器标记特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BrServiceControllerAttribute : Attribute
{
	public const string Description = "这是一个长连接服务控制器";
}