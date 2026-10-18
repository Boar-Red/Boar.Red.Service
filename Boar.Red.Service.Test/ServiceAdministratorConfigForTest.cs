using BaoXia.Utils;
using Boar.Red.Service.Models;

namespace Boar.Red.Service.Test;

public class ServiceAdministratorConfigForTest : ConfigFile
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ServiceAdministratorConfig ServiceAdministratorConfig { get; set; } = new();

	#endregion
}
