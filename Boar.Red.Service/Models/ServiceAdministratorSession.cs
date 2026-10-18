using System.Security.Cryptography;

namespace Boar.Red.Service.Models;

public class ServiceAdministratorSession(string account, string password, string? connectionIpEndPoints, DateTime createTime)
{
    ////////////////////////////////////////////////
    // @自身属性
    ////////////////////////////////////////////////

    #region 自身属性

    public string Token { get; set; } = GenerateToken();

    public string Account { get; set; } = account;

    public string Password { get; set; } = password;

    public string ConnectionIpEndPoints { get; set; } = connectionIpEndPoints ?? string.Empty;

    public DateTime CreateTime { get; set; } = createTime;

    #endregion



    ////////////////////////////////////////////////
    // @静态变量
    ////////////////////////////////////////////////

    #region 静态变量

    public static string GenerateToken()
    {
	var tokenBytes = RandomNumberGenerator.GetBytes(32);
	var token = Convert.ToBase64String(tokenBytes);
	{ }
	return token;
    }

    #endregion
}
