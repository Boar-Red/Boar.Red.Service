using Boar.Red.Service.WebService.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Boar.Red.Service.WebService.Extensions;

public static class HttpRequestBodySizeMaxMiddelwareExtension
{
	public static IApplicationBuilder UseHttpRequestBodySizeMax(
	    this IApplicationBuilder builder,
	    Func<long> toGetHttpRequestBodySizeMax)
	{
		return builder.UseMiddleware<HttpRequestBodySizeMaxMiddelware>(toGetHttpRequestBodySizeMax);
	}
}
