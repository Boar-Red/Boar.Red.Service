namespace Boar.Red.Service.BrService.Constants
{
	public enum ItemFoundType
	{
		/// <summary>
		/// 没有发现。
		/// </summary>
		NotFound,

		/// <summary>
		/// 普通发现。
		/// </summary>
		NormalFound,

		/// <summary>
		/// 发现了，新的服务节点信息。
		/// </summary>
		NewItemFound,

		/// <summary>
		/// 发现了，服务节点信息更新。
		/// </summary>
		UpdatedItemFound
	}
}
