using Microsoft.EntityFrameworkCore;

namespace Boar.Red.Service.Test.NetCoreMvc.Data;

/// <summary>
/// 默认数据库上下文，常用命令：
/// Add-Migration InitialCreate ，初始化数据库。
/// 设置迁移文件中的”AutomaticMigrationsEnabled=true“，开启自动迁移。
/// Enable-Migrations，启用自动迁移。
/// Add-Migration，为挂起的Model变化添加迁移脚本。
/// Update-Database，将挂起的迁移更新到数据库。
/// Update-Database -Verbose，将模型更新到数据库中并显示更新脚本，可将更新脚本复制下来在生产环境中运行。
/// Get-Migrations，获取已经应用的迁移。
/// </summary>
public class TestNetCoreMvctConfigDbContext
	(DbContextOptions<TestNetCoreMvctConfigDbContext> options) : DbContext(options)
{
}