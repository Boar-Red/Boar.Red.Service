using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace Boar.Red.Service.WebService.Models;

public class HttpsCertificateFileInfo
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	/// <summary>
	/// 证书文件地址。
	/// </summary>
	public string? HostName { get; set; }

	/// <summary>
	/// 证书文件路径。
	/// </summary>
	public string? CertificateFilePath { get; set; }

	/// <summary>
	/// 证书密码文件路径。
	/// </summary>
	public string? CertificatePasswordFilePath { get; set; }

	/// <summary>
	/// 证书密码。
	/// </summary>
	public string? CertificateFilePassword { get; set; }

	/// <summary>
	/// 证书文件过期时间。
	/// </summary>
	public DateTimeOffset CertificateFileExpireTime { get; set; }

	/// <summary>
	/// 对应的证书缓存。
	/// </summary>
	[JsonIgnore]
	public X509Certificate2? X509Certificate2 { get; set; }


	#endregion
}