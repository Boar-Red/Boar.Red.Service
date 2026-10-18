using BaoXia.Utils;

namespace Boar.Red.Service.BrService.ViewModels;

public class ServiceAdministratorInitializationParam
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public ConfigFile? ConfigFile { get; set; }

	public string? ConfigPropertyNameInConfigFile { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public ServiceAdministratorInitializationParam(
	    ConfigFile? configFile,
	    string? configPropertyNameInConfigFile)
	{
		ConfigFile = configFile;
		ConfigPropertyNameInConfigFile = configPropertyNameInConfigFile;
	}

	#endregion
}
