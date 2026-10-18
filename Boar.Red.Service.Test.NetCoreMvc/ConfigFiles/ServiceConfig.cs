using BaoXia.Utils;
using Boar.Red.Service.Models;
using Boar.Red.Service.WebService.Models;

namespace Boar.Red.Service.Test.NetCoreMvc.ConfigFiles
{
	public class ServiceConfig : ConfigFile
	{
		////////////////////////////////////////////////
		// @自身属性，服务基础参数
		////////////////////////////////////////////////

		#region 自身属性，服务基础参数

		/// <summary>
		/// TestNetCoreMvctConfig配置。
		/// </summary>
		public TestNetCoreMvctConfig? TestNetCoreMvctConfig { get; set; }

		/// <summary>
		/// 服务管理员配置。
		/// </summary>
		public ServiceAdministratorConfig? ServiceAdministratorConfig { get; set; }

		/// <summary>
		/// Web服务基础配置。
		/// </summary>
		public WebServiceConfig? WebServiceConfig { get; set; }

		/// <summary>
		/// 长连接服务配置。
		/// </summary>
		public BrService.Models.ServiceConfig? BrServiceConfig { get; set; }



		#endregion
	}
}