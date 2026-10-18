using Boar.Red.Service.WebService.Middlewares;

namespace Boar.Red.Service.Test;

[TestClass]
public class HttpRequestCrossDomainControlMiddlewareTest
{
	static readonly string[] _credibleDomainsWithNone = [];
	static readonly string[] _credibleDomains =
	[
		"boar.red"
	];

	static readonly string[] _validDomains = [
		"boar.red",
		"http://boar.red",
		"https://boar.red",
		"http://www.boar.red",
		"https://www.boar.red",
		"http://www.boar.red:80",
		"https://www.boar.red:443",
                        //
                        "http://boar.red/",
		"https://boar.red/",
		"http://www.boar.red/",
		"https://www.boar.red/",
		"http://www.boar.red:80/",
		"https://www.boar.red:443/",
                        //
                        "http://boar.red/page",
		"https://boar.red/page",
		"http://www.boar.red/page",
		"https://www.boar.red/page",
		"http://www.boar.red:80/page",
		"https://www.boar.red:443/page",
                        //
                        "http://boar.red/page/",
		"https://boar.red/page/",
		"http://www.boar.red/page/",
		"https://www.boar.red/page/",
		"http://www.boar.red:80/page/",
		"https://www.boar.red:443/page/",
                        //
                        "http://boar.red/page/index",
		"https://boar.red/page/index",
		"http://www.boar.red/page/index",
		"https://www.boar.red/page/index",
		"http://www.boar.red:80/page/index",
		"https://www.boar.red:443/page/index",
                        //
                        "http://boar.red/page/index/",
		"https://boar.red/page/index/",
		"http://www.boar.red/page/index/",
		"https://www.boar.red/page/index/",
		"http://www.boar.red:80/page/index/",
		"https://www.boar.red:443/page/index/",
                        ////////////////////////////////////////////////
                ];

	static readonly string[] _invalidDomains = [
		"abc.com",
		"http://abc.com",
		"https://abc.com",
		"http://www.abc.com",
		"https://www.abc.com",
		"http://www.abc.com:80",
		"https://www.abc.com:443",
                        //
                        "http://abc.com/",
		"https://abc.com/",
		"http://www.abc.com/",
		"https://www.abc.com/",
		"http://www.abc.com:80/",
		"https://www.abc.com:443/",
                        //
                        "http://abc.com/page",
		"https://abc.com/page",
		"http://www.abc.com/page",
		"https://www.abc.com/page",
		"http://www.abc.com:80/page",
		"https://www.abc.com:443/page",
                        //
                        "http://abc.com/page/",
		"https://abc.com/page/",
		"http://www.abc.com/page/",
		"https://www.abc.com/page/",
		"http://www.abc.com:80/page/",
		"https://www.abc.com:443/page/",
                        //
                        "http://abc.com/page/index",
		"https://abc.com/page/index",
		"http://www.abc.com/page/index",
		"https://www.abc.com/page/index",
		"http://www.abc.com:80/page/index",
		"https://www.abc.com:443/page/index",
                        //
                        "http://abc.com/page/index/",
		"https://abc.com/page/index/",
		"http://www.abc.com/page/index/",
		"https://www.abc.com/page/index/",
		"http://www.abc.com:80/page/index/",
		"https://www.abc.com:443/page/index/",
                        ////////////////////////////////////////////////
                ];

	static readonly string[] _testQueryParams = [
		"",
		"?id=1",
		"?id=1&type=2"
	];

	[TestMethod]
	public void IsDomainInOrSubdomainOfDomainList_WithNoneDomainValid_Test()
	{
		string[]? credibleDomains = _credibleDomainsWithNone;
		foreach (var testQueryParam in _testQueryParams)
		{
			foreach (var domain in _validDomains)
			{
				var finalDomain = domain + testQueryParam;

				if (HttpRequestCrossDomainControlMiddleware
				.IsDomainInOrSubdomainOfDomainList(
					finalDomain,
					credibleDomains))
				{
					Assert.Fail();
				}
			}

			foreach (var domain in _invalidDomains)
			{
				var finalDomain = domain + testQueryParam;

				if (HttpRequestCrossDomainControlMiddleware
				.IsDomainInOrSubdomainOfDomainList(
					finalDomain,
					credibleDomains))
				{
					Assert.Fail();
				}
			}
		}
	}

	[TestMethod]
	public void IsDomainInOrSubdomainOfDomainList_WithDomainsSpecifiedValid_Test()
	{
		string[]? credibleDomains = _credibleDomains;
		foreach (var testQueryParam in _testQueryParams)
		{
			foreach (var domain in _validDomains)
			{
				var finalDomain = domain + testQueryParam;

				if (!HttpRequestCrossDomainControlMiddleware
				.IsDomainInOrSubdomainOfDomainList(
					finalDomain,
					credibleDomains))
				{
					Assert.Fail();
				}
			}

			foreach (var domain in _invalidDomains)
			{
				var finalDomain = domain + testQueryParam;

				if (HttpRequestCrossDomainControlMiddleware
				.IsDomainInOrSubdomainOfDomainList(
					finalDomain,
					credibleDomains))
				{
					Assert.Fail();
				}
			}
		}
	}

}
