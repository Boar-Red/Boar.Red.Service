using BaoXia.Utils.Extensions;
using BaoXia.Utils.Interfaces;
using Boar.Red.Service.BrService.Models;
using System.Security.Cryptography.X509Certificates;

namespace Boar.Red.Service.WebService.Models;

public class WebServiceConfig
{
	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

	private static HttpsCertificateFileInfo[]? _httpsCertificateFileInfesCache = null;

	#endregion


	////////////////////////////////////////////////
	// @静态常量
	////////////////////////////////////////////////

	#region 静态常量

	public static readonly WebServiceConfig Default = new();

	#endregion


	////////////////////////////////////////////////
	// @自身属性，Web服务基础配置。
	////////////////////////////////////////////////

	#region 自身属性，Web服务基础配置。

	/// <summary>
	/// Web服务根路径Uri。
	/// </summary>
	public string? WebServiceRootPathUri { get; set; }

	/// <summary>
	/// Http协议绑定的IP地址。
	/// </summary>
	public string? HttpIPAddress { get; set; }

	/// <summary>
	/// Http协议端口号。
	/// </summary>
	public int HttpPort { get; set; }

	/// <summary>
	/// Https协议绑定的IP地址。
	/// </summary>
	public string? HttpsIPAddress { get; set; }

	/// <summary>
	/// Https协议端口号。
	/// </summary>
	public int HttpsPort { get; set; }

	/// <summary>
	/// Https证书，文件路径。
	/// </summary>
	public string? HttpsCertificateFilePath { get; set; }

	/// <summary>
	/// Https证书密码，文件路径。
	/// </summary>
	public string? HttpsCertificatePasswordFilePath { get; set; }

	/// <summary>
	/// Https证书密码。
	/// </summary>
	public string? HttpsCertificateFilePassword { get; set; }

	/// <summary>
	/// Https多个证书文件信息。
	/// </summary>
	public HttpsCertificateFileInfo[]? HttpsCertificateFileInfes { get; set; }

	/// <summary>
	/// 是否Https为必须，默认为：false。
	/// </summary>
	public bool IsHttpsNecessary { get; set; } = false;

	/// <summary>
	/// 是否禁用静态页面。
	/// </summary>
	public bool IsStaticFilesDisable { get; set; } = false;

	/// <summary>
	/// 默认页面的相对路径。
	/// </summary>
	public string? DefaultPageRelativePath { get; set; }

	/// <summary>
	/// 必须要的Http头部参数。
	/// </summary>
	public string[]? HttpHeaderKeysForValueNecessary { get; set; }

	/// <summary>
	/// 用于Http请求的，跨域控制信息。
	/// </summary>
	public CrossDomainControlInfo? HttpCrossDomainControlInfo { get; set; }

	/// <summary>
	/// RequestBody大小的最大值，单位为“MB”。
	/// </summary>
	public long HttpRequestBodySizeInMBMax { get; set; }

	/// <summary>
	/// RequestBody大小的最大值，单位为“字节”。
	/// </summary>
	public long HttpRequestBodySizeInBytesMax => HttpRequestBodySizeInMBMax * 1024 * 1024;

	/// <summary>
	/// 用于WebSocket请求的，跨域控制信息，注意：可信任的来源域名数组，不支持模糊匹配。
	/// </summary>
	public CrossDomainControlInfo? WebSocketCrossDomainControlInfo { get; set; }

	/// <summary>
	/// WebSocket长连接，保持服务连接的心跳包间隔秒数，用于控制连接的状态变化敏感度。
	/// </summary>
	public double WebSocketKeepAliveIntervalSeconds { get; set; } = 1.0;

	/// <summary>
	/// WebSocket保活Ping等待Pong的超时秒数，默认为30秒；小于等于0时保留不检查Pong超时的行为。
	/// </summary>
	public double WebSocketKeepAliveTimeoutSeconds { get; set; } = 30.0;

	/// <summary>
	/// WebSocket单次发送的超时秒数，默认为30秒；无效值使用默认值，超时后中断连接。
	/// </summary>
	public double WebSocketSendTimeoutSeconds { get; set; } = 30.0;

	/// <summary>
	/// WebSocket关闭握手的超时秒数，默认为5秒；无效值使用默认值，超时后中断连接。
	/// </summary>
	public double WebSocketCloseTimeoutSeconds { get; set; } = 5.0;

	/// <summary>
	/// WebSocket连接的“鉴权截止时限”秒数：连接建立后，必须在此时限内完成鉴权，
	/// 否则视为无效连接，由服务端主动断开。
	/// </summary>
	public double? WebSocketAuthenticationDeadlineSeconds { get; set; } = 10.0;

	#endregion


	////////////////////////////////////////////////
	// @自身属性，安全相关。
	////////////////////////////////////////////////

	#region 自身属性，安全相关。

	/// <summary>
	/// Csrf令牌管理器配置。
	/// </summary>
	public CsrfTokenManagerConfig? CsrfTokenManagerConfig { get; set; }

	#endregion


	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	static string? ToGetValidCertificateFilePath(string? httpsCertificateFilePath)
	{
		if (string.IsNullOrEmpty(httpsCertificateFilePath))
		{
			return null;
		}
		if (!System.IO.Path.IsPathRooted(httpsCertificateFilePath))
		{
			httpsCertificateFilePath = System.IO.Path.Combine(
				string.IsNullOrEmpty(BaoXia.Utils.Environment.ApplicationDirectoryPath) ? AppContext.BaseDirectory : BaoXia.Utils.Environment.ApplicationDirectoryPath,
				httpsCertificateFilePath);
		}
		if (System.IO.File.Exists(httpsCertificateFilePath))
		{
			return httpsCertificateFilePath;
		}
		return null;
	}

	public static HttpsCertificateFileInfo[]? SetHttpsCertificateFileInfesCache(
		HttpsCertificateFileInfo[]? httpsCertificateFileInfesCache,
		ILogFile? exceptionLogFile
	)
	{
		var validHttpsCertificateFileInfo = new List<HttpsCertificateFileInfo>();
		if (httpsCertificateFileInfesCache.IsNotEmpty())
		{
			foreach (var httpsCertificateFileInfo in httpsCertificateFileInfesCache)
			{
				try
				{
					if (httpsCertificateFileInfo.X509Certificate2 != null)
					{
						continue;
					}
					if (
						ToGetValidCertificateFilePath(
							httpsCertificateFileInfo.CertificateFilePath
						)
						is not { } validCertificateFilePath
					)
					{
						continue;
					}

					var certificateFilePassword =
						httpsCertificateFileInfo.CertificateFilePassword;
					if (string.IsNullOrEmpty(certificateFilePassword))
					{
						var certificatePasswordFilePath =
							httpsCertificateFileInfo.CertificatePasswordFilePath;
						if (
							string.IsNullOrEmpty(
								certificatePasswordFilePath
							) != true
						)
						{
							certificateFilePassword = System
								.IO.File.ReadAllText(
									ToGetValidCertificateFilePath(certificatePasswordFilePath)
									?? throw new System.IO.FileNotFoundException("证书密码文件不存在。")
								)
								?.Trim();
						}
					}

					var x509Certificate2 =
						X509CertificateLoader.LoadPkcs12FromFile(
							validCertificateFilePath,
							certificateFilePassword
						);
					if (x509Certificate2 == null)
					{
						continue;
					}
					////////////////////////////////////////////////
					// !!!
					httpsCertificateFileInfo.X509Certificate2 =
						x509Certificate2;
					// !!!
					validHttpsCertificateFileInfo.Add(httpsCertificateFileInfo);
					// !!!
					////////////////////////////////////////////////
				}
				catch (Exception exception)
				{
					exceptionLogFile?.Logs(
						nameof(WebServiceConfig),
						"WebServiceExtension设置Https证书文件信息缓存失败，程序异常。",
						exception
					);
				}
			}
		}
		httpsCertificateFileInfesCache = [.. validHttpsCertificateFileInfo];
		// !!!
		_httpsCertificateFileInfesCache = httpsCertificateFileInfesCache;
		// !!!
		return httpsCertificateFileInfesCache;
	}

	public static HttpsCertificateFileInfo[]? GetHttpsCertificateFileInfesCache()
	{
		return _httpsCertificateFileInfesCache;
	}

	#endregion
}
