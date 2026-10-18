using BaoXia.Constants;
using BaoXia.Utils.Models;
using Boar.Red.Service.Models;
using Boar.Red.Service.Services;
using System.Net;

namespace Boar.Red.Service.Test;

[TestClass]
public class ServiceAdministratorServiceTest
{
	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	private static ServiceAdministratorConfigForTest CreateConfig(int sessionsCountMax)
	{
		return new ServiceAdministratorConfigForTest
		{
			ServiceAdministratorConfig = new ServiceAdministratorConfig
			{
				SessionsCountMax = sessionsCountMax,
				Administrators = new[] { "admin", "other-admin" }.Select(account => new ServiceAdministrator
				{
					Account = account,
					Password = "test-password",
					ExpireTime = DateTime.Now.AddDays(1)
				}).ToArray()
			}
		};
	}

	private static ServiceAdministratorService CreateService(ServiceAdministratorConfigForTest config)
	{
		return new ServiceAdministratorService(config, nameof(config.ServiceAdministratorConfig), null, null, null);
	}

	private static ServiceAdministratorSession Login(ServiceAdministratorService service, string account = "admin")
	{
		var endPoint = new IPEndPoint(IPAddress.Loopback, 12345);
		var connection = new ConnectionIpEndPoints([endPoint], null, null, null, endPoint);
		Assert.AreEqual(Error.None, service.Login(account, "test-password", connection, out var session));
		Assert.IsNotNull(session);
		return session;
	}

	[TestMethod]
	public void Login_WhenLimitIsOne_RevokesPreviousToken()
	{
		using var config = CreateConfig(1);
		var service = CreateService(config);
		var first = Login(service);
		var second = Login(service);

		Assert.AreEqual(Error.UserTokenNotExisted, service.GetValidSessionWithToken(first.Token, "127.0.0.1").Error);
		Assert.IsTrue(service.GetValidSessionWithToken(second.Token, "127.0.0.1").IsSuccess);
	}

	[TestMethod]
	public void Login_WhenLimitIsTwo_RevokesOldestByCreationTime()
	{
		using var config = CreateConfig(2);
		var service = CreateService(config);
		var newer = Login(service);
		newer.CreateTime = DateTime.Now.AddMinutes(-1);
		var oldest = Login(service);
		oldest.CreateTime = DateTime.Now.AddMinutes(-2);
		var latest = Login(service);

		Assert.AreEqual(Error.UserTokenNotExisted, service.GetValidSessionWithToken(oldest.Token, "127.0.0.1").Error);
		Assert.IsTrue(service.GetValidSessionWithToken(newer.Token, "127.0.0.1").IsSuccess);
		Assert.IsTrue(service.GetValidSessionWithToken(latest.Token, "127.0.0.1").IsSuccess);
	}

	[TestMethod]
	public void Login_WhenOneAccountExceedsLimit_KeepsOtherAccountSession()
	{
		using var config = CreateConfig(1);
		var service = CreateService(config);
		var other = Login(service, "other-admin");
		var first = Login(service);
		var latest = Login(service);

		Assert.AreEqual(Error.UserTokenNotExisted, service.GetValidSessionWithToken(first.Token, "127.0.0.1").Error);
		Assert.IsTrue(service.GetValidSessionWithToken(other.Token, "127.0.0.1").IsSuccess);
		Assert.IsTrue(service.GetValidSessionWithToken(latest.Token, "127.0.0.1").IsSuccess);
	}

	[TestMethod]
	public void Login_WhenLimitIsZero_KeepsExistingSessions()
	{
		using var config = CreateConfig(0);
		var service = CreateService(config);
		var sessions = new[] { Login(service), Login(service), Login(service) };

		foreach (var session in sessions)
		{
			Assert.IsTrue(service.GetValidSessionWithToken(session.Token, "127.0.0.1").IsSuccess);
		}
	}

	#endregion
}
