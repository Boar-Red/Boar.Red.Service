namespace Boar.Red.Service.Test.NetCoreMvc.Constants
{
	/// <summary>
	/// 元素缓存命名空间。
	/// </summary>
	public class ItemCacheNamespaces
	{

		////////////////////////////////////////////////
		// @静态变量
		////////////////////////////////////////////////

		#region 静态变量

		/// <summary>
		/// 样例缓存命名空间：项目缓存的命名空间前缀。
		/// </summary>
		public const string Prefix = "Boar.Red.Service.Test.NetCoreMvc.";

		/// <summary>
		/// 样例缓存命名空间：X模型信息缓存的命名空间。
		/// </summary>
		public static readonly string XModel = Namespace("XModel");

		#endregion

		////////////////////////////////////////////////
		// @类方法
		////////////////////////////////////////////////

		#region 类方法

		public static string Namespace(string @namespace)
		{
			return Prefix + @namespace;
		}

		#endregion
	}
}
