using BaoXia.Utils;
using BaoXia.Utils.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Boar.Red.Service.WebService.Middlewares
{
	public class HttpRequestBodySizeMaxMiddelware : MiddlewareAsync
	{
		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性			

		public Func<long> ToGetHttpRequestBodySizeMax { get; set; }

		#endregion



		public HttpRequestBodySizeMaxMiddelware(
		    Func<long> toGetHttpRequestBodySizeMax,
		    ILogFile? logFile,
		    ILogFile? exceptionLogFile,
		    ILogFile? warningLogFile)
		    : base(logFile,
		      exceptionLogFile,
		      warningLogFile)
		{
			this.ToGetHttpRequestBodySizeMax = toGetHttpRequestBodySizeMax;
		}

		////////////////////////////////////////////////
		// @实现“Middleware”定义方法
		////////////////////////////////////////////////

		#region 实现“Middleware”

		protected override async Task<bool> DidIfGoToNextMiddlewareByInvokeAsync(HttpContext httpContext)
		{
			var httpRequestBodySizeMax = 0L;
			if (ToGetHttpRequestBodySizeMax != null)
			{
				httpRequestBodySizeMax = ToGetHttpRequestBodySizeMax();
			}
			if (httpRequestBodySizeMax > 0)
			{
				var request = httpContext.Request;
				var requestContentLength = request.Headers.ContentLength;
				if (requestContentLength > httpRequestBodySizeMax)
				{
					var response = httpContext.Response;
					{
						response.StatusCode = (int)System.Net.HttpStatusCode.RequestEntityTooLarge;
						await response.CompleteAsync();
					}
					return false;
				}
			}
			return true;
		}

		#endregion
	}
}
