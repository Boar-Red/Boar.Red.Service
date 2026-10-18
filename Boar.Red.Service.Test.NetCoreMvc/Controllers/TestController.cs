using BaoXia.Constants;
using BaoXia.Constants.Models;
using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService;
using Boar.Red.Service.Test.NetCoreMvc.Interfaces;
using Boar.Red.Service.Test.NetCoreMvc.Services;
using Boar.Red.Service.Test.NetCoreMvc.Services.Base;
using Microsoft.AspNetCore.Mvc;

namespace Boar.Red.Service.Test.NetCoreMvc.Controllers;

public class TESTController : Controller
{
	[HttpGet]
	public async Task<IActionResult> Index()
	{
		var httpClient = new HttpClient();

		////////////////////////////////////////////////
		// 测试：服务注入功能。
		////////////////////////////////////////////////
		var testClassWithoutBaseClass
			= BrServiceTypesRegistered.GetServiceTypesWithType(typeof(TestServcieWithoutBase));
		if (testClassWithoutBaseClass == null)
		{
			throw new Exception("服务注入功能测试失败，没有找到对应数量的“testClassesWithBaseClass”。");
		}

		var testClassesWithBaseClass
			= BrServiceTypesRegistered.GetServiceTypesWithBaseClass(typeof(BaseService));
		if (testClassesWithBaseClass?.Count != 2)
		{
			throw new Exception("服务注入功能测试失败，没有找到对应数量的“testClassesWithBaseClass”。");
		}

		var testClassesWithInterface
			= BrServiceTypesRegistered.GetServiceTypesWithInterface(typeof(TestInterface));
		if (testClassesWithInterface?.Count != 2)
		{
			throw new Exception("服务注入功能测试失败，没有找到对应数量的“testClassesWithInterface”。");
		}



		////////////////////////////////////////////////
		// 测试：POST请求。
		////////////////////////////////////////////////

		var response = await httpClient.PostToJsonApiAsync<TestResponse>(
			"http://localhost:8001/test/test",
			null);
		if (response?.IsSuccess == true)
		{
			throw new Exception("Api请求框架，请求有效性验证测试失败：没有拦截无效的请求。");
		}

		response = await httpClient.PostToJsonApiAsync<TestResponse>(
			"http://localhost:8001/test/test",
			new TestRequest()
			{
				Name = "Abc",
				Age = 1
			});
		if (response?.IsSuccess != true)
		{
			throw new Exception("Api请求框架，请求有效性验证测试失败：拦截了有效的请求。");
		}

		return new StatusCodeResult((int)System.Net.HttpStatusCode.OK);
	}

	public class TestRequest : Request
	{
		public string? Name { get; set; }

		public int Age { get; set; }

		protected override BaoXia.Constants.Models.Response? DidGetErrorResonseByCheckRequest(RequestPipelineType requestPipelineType)
		{
			var errorResponse = base.DidGetErrorResonseByCheckRequest(requestPipelineType);
			if (errorResponse?.IsFailed == true)
			{
				return errorResponse;
			}

			if (string.IsNullOrWhiteSpace(Name))
			{
				return new()
				{
					Error = Error.InvalidRequest,
					ErrorDescription = $"请求无效，{nameof(Name)}不能为“空”。"
				};
			}

			if (Age < 1)
			{
				return new()
				{
					Error = Error.InvalidRequest,
					ErrorDescription = $"请求无效，{nameof(Age)}不能小于“1”。"
				};
			}

			return null;
		}
	}

	public class TestResponse : Response
	{
		public string? Name { get; set; }

		public int Age { get; set; }
	}

	[HttpPost]
	public IActionResult Test([FromBody] TestRequest request)
	{
		return Json(new TestResponse()
		{
			Name = request.Name,
			Age = request.Age
		});
	}
}