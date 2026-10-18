namespace Boar.Red.Service.Constants
{
	public enum ServiceLifeCycle
	{
		/// <summary>
		/// 暂时，的服务。
		/// </summary>
		Transient = 0,

		/// <summary>
		/// 作用域，的服务。
		/// </summary>
		Scoped = 1,

		/// <summary>
		/// 单例，的服务。
		/// </summary>
		Singleton = 2
	}
}
