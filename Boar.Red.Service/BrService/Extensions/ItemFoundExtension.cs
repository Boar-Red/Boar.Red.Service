using Boar.Red.Service.BrService.Constants;

namespace Boar.Red.Service.BrService.Extensions
{
	public static class ItemFoundExtension
	{

		////////////////////////////////////////////////
		// @类方法
		////////////////////////////////////////////////

		#region 类方法

		public static bool IsNotOrNormalFound(
		    this ItemFoundType itemFoundType)
		{
			if (itemFoundType == ItemFoundType.NotFound
			    || itemFoundType == ItemFoundType.NormalFound)
			{
				return true;
			}
			return false;
		}

		public static bool IsNewOrUpdatedFound(
		    this ItemFoundType itemFoundType)
		{
			if (itemFoundType == ItemFoundType.NewItemFound
			    || itemFoundType == ItemFoundType.UpdatedItemFound)
			{
				return true;
			}
			return false;
		}


		#endregion

	}
}
