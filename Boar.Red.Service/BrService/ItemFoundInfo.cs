using Boar.Red.Service.BrService.Constants;

namespace Boar.Red.Service.BrService;

public class ItemFoundInfo<ItemType> where ItemType : class
{

	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ItemType Item { get; set; }

	public ItemFoundType FoundType { get; set; }

	#endregion

	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ItemFoundInfo(
	    ItemType item,
	    ItemFoundType itemFoundType)
	{
		Item = item;
		FoundType = itemFoundType;
	}

	#endregion
}
