namespace Boar.Red.Service.BrService.Interfaces;

public class IServiceApplicationInfo
{
	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public int Id { get; set; }

	public string Identity { get; set; } = default!;

	public string Name { get; set; } = default!;

	public string? Description { get; set; }


	#endregion
}