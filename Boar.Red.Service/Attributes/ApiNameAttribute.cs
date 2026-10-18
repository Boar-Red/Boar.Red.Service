namespace Boar.Red.Service.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ApiNameAttribute(string? name, string? description = null, string? logContentForException = null)
    : Attribute
{

	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string? Name { get; set; } = name;

	public string? Description { get; set; } = description;

	public string? LogContentForException { get; set; } = logContentForException;

	#endregion
}
