using Boar.Red.Service.BrService.ViewModels;
using Boar.Red.Service.Constants;
using Boar.Red.Service.Extensions;
using Boar.Red.Service.Test.NetCoreMvc.ConfigFiles;
using Boar.Red.Service.Test.NetCoreMvc.Constants;
using Boar.Red.Service.Test.NetCoreMvc.Data;
using Boar.Red.Service.Test.NetCoreMvc.LogFiles;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var appBuilder = WebApplication.CreateBuilder(args);
var httpServer = HttpServer.Kestrel;
#if WEBSERVER_IIS
httpServer = HttpServer.IIS;
#endif

appBuilder.RunWithServerName(
#if DEBUG
	"Development Server",
#else
	"Production Server",
#endif
	BaoXia.Utils.Environment.GetEnvironmentNameWith_ASPNETCORE_ENVIRONMENT(),
	Assembly.GetExecutingAssembly(),
	httpServer,
	//
	AESKeys.Default,
	//
	"ConfigFiles",
	"LogFiles",
	//
	Log.Logs,
	Log.Exception,
	Log.Warning,
	Log.BrService,
	//
	Config.Service,
	nameof(ServiceConfig.WebServiceConfig),
	//
	RouteTemplateConstants.RouteTemplatesDefault,
	//
	new ServiceInitializationParam(
		//
		Config.Service,
		nameof(ServiceConfig.WebServiceConfig),
		Config.Service,
		nameof(ServiceConfig.BrServiceConfig),
		//
		true,
		false,
		false,
		new Boar.Red.Service.BrService.ServiceDiscoverer()),
	new ServiceAdministratorInitializationParam(
		Config.Service,
		nameof(ServiceConfig.ServiceAdministratorConfig)),
	(appBuilder) =>
	{
		////////////////////////////////////////////////
		// 1/4�����÷���ͻ��ˣ����ݿ����
		////////////////////////////////////////////////
		appBuilder
		.Services
		.AddDbContext<TestNetCoreMvctConfigDbContext>(options =>
		{
			var dbConnectionString
			= Config.Service.TestNetCoreMvctConfig?.DbConnectionString
			?? throw new InvalidOperationException(
				"�������ݿ����ʧ�ܣ�û���������ļ������á����ݿ������ַ�������DbConnectionString����");
			// !!!
			options.UseSqlServer(dbConnectionString);
			// !!!
		});

		return appBuilder;
	});