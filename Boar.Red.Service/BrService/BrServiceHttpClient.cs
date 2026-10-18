namespace Boar.Red.Service.BrService;

public class BrServiceHttpClient
{

	////////////////////////////////////////////////
	// @静态常量
	////////////////////////////////////////////////

	#region 静态常量

	public const string UserAgent = "BrService/1.0.0 (ServiceDiscoverer)";

	#endregion

	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

	private static readonly object _httpClientLocker = new();

	private static HttpClient? _httpClient = null;

	public static HttpClient HttpClient
	{
		get
		{
			if (_httpClient != null)
			{
				return _httpClient;
			}

			lock (_httpClientLocker)
			{
				if (_httpClient != null)
				{
					return _httpClient;
				}

				_httpClient = new HttpClient();
				{
					_httpClient.DefaultRequestHeaders
					    .UserAgent
					    .TryParseAdd(UserAgent);
				}
			}
			return _httpClient;
		}
	}

	#endregion



}
