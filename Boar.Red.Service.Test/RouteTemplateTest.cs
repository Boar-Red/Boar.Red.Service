
using BaoXia.Utils.Extensions;
using Boar.Red.Service.Models;

namespace Boar.Red.Service.Test
{
	[TestClass]
	public class RouteTemplateTest
	{
		[TestMethod]
		public void SingleMatchTest()
		{
			var routeTemplate = new RouteTemplate(
				"default",
				"{controller}/{action}/{id?=1}");
			var routeInfo = routeTemplate.GetRouteInfoWithUrlPath(
				"home/index");
			Assert.IsTrue(routeInfo != null);
			{
				Assert.IsTrue(routeInfo.ControllerName.Equals("home"));
				Assert.IsTrue(routeInfo.ActionName.Equals("index"));
			}
		}

		class TestItem
		{
			public string RouteTemplate { get; set; }

			public string TestUrl { get; set; }

			public string ControllerName { get; set; }

			public string ActionName { get; set; }

			public Dictionary<string, string> ActionParams { get; set; }

			public bool MatchedResult { get; set; }

			public TestItem(
				string routeTemplate,
				string testUrl,
				string controllerName,
				string actionName,
				Dictionary<string, string> actionParams,
				bool matchedResult)
			{
				RouteTemplate = routeTemplate;
				TestUrl = testUrl;
				ControllerName = controllerName;
				ActionName = actionName;
				ActionParams = actionParams;
				MatchedResult = matchedResult;
			}
		}

		void TestWithItems(TestItem[] testItems)
		{
			foreach (var testItem in testItems)
			{
				var routeTemplate = new RouteTemplate(
					"default",
					testItem.RouteTemplate);
				var routeInfo = routeTemplate.GetRouteInfoWithUrlPath(
					testItem.TestUrl);
				if (testItem.MatchedResult)
				{
					Assert.IsTrue(routeInfo != null);
					Assert.IsTrue(routeInfo.ControllerName.EqualsIgnoreCase(testItem.ControllerName));
					Assert.IsTrue(routeInfo.ActionName.EqualsIgnoreCase(testItem.ActionName));
					var testActionParams = testItem.ActionParams;
					Assert.IsTrue(testActionParams.Count <= (routeInfo.ActionParams!.Count - 2));
					if (testActionParams != null)
					{
						foreach (var testActionParam in testActionParams)
						{
							var routeInfoActionValue = routeInfo.ActionParams[testActionParam.Key];
							{
								Assert.IsTrue(routeInfoActionValue.EqualsIgnoreCase(testActionParam.Value));
							}
						}
					}
				}
				else
				{
					Assert.IsTrue(routeInfo == null);
					//Assert.IsTrue(routeInfo.ControllerName.EqualsIgnoreCase(testItem.ControllerName));
					//Assert.IsTrue(routeInfo.ActionName.EqualsIgnoreCase(testItem.ActionName));
					//var testActionParams = testItem.ActionParams;
					//Assert.IsTrue(testActionParams.Count == (routeInfo.ActionParams!.Count - 2));
					//if (testActionParams != null)
					//{
					//        foreach (var testActionParam in testActionParams)
					//        {
					//                var routeInfoActionValue = routeInfo.ActionParams[testActionParam.Key];
					//                {
					//                        Assert.IsTrue(routeInfoActionValue.EqualsIgnoreCase(testActionParam.Value));
					//                }
					//        }
					//}
				}
			}
		}

		[TestMethod]
		public void RouteTemplatesMatch_01_Test()
		{
			var testItems = new TestItem[]
			{
				new TestItem("{controller}/{action}",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				true),

				new TestItem("/{controller}/{action}",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				true),

				new TestItem("{controller}/{action}/",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				true),

				new TestItem("/{controller}/{action}/",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				true),
				////////////////////////////////////////////////
                                new TestItem("{controller}/{action}",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				true),

				new TestItem("{controller}/{action}",
				"/home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				true),

				new TestItem("{controller}/{action}",
				"home/index/",
				"home",
				"index",
				new Dictionary<string, string>(),
				true),

				new TestItem("{controller}/{action}",
				"/home/index/",
				"home",
				"index",
				new Dictionary<string, string>(),
				true),
			};

			// !!!
			TestWithItems(testItems);
			// !!!
		}

		[TestMethod]
		public void RouteTemplatesMatch_02_Test()
		{
			var testItems = new TestItem[]
			{
				new TestItem("{controller}/{action}/{id=0}",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				false),
				new TestItem("{controller}/{action}/{id=0}/",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				false),

				new TestItem("{controller}/{action}/{id?=0}",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				true),
				new TestItem("{controller}/{action}/{id?=0}/",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				true),

				new TestItem("{controller}/{action}/{id?=0}/{pageIndex?=0}/{pageSize?=20}",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				true),
				new TestItem("{controller}/{action}/{id?=0}/{pageIndex?=0}/{pageSize?=20}/",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				true),

				new TestItem("{controller}/{action}/{id?=0}/{pageIndex=0}",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				false),
				new TestItem("{controller}/{action}/{id?=0}/{pageIndex=0}/",
				"home/index",
				"home",
				"index",
				new Dictionary<string, string>(),
				false)
			};

			// !!!
			TestWithItems(testItems);
			// !!!
		}

		[TestMethod]
		public void RouteTemplatesMatch_03_Test()
		{
			var testItems = new TestItem[]
			{
				new TestItem("{controller}/{action}/{*requestParams}",
				"home/index/session/login",
				"home",
				"index",
				new Dictionary<string, string>()
				{
					{ "requestParams", "session/login"}
				},
				true),

				////////////////////////////////////////////////
                                // !!!
                                new TestItem("{controller}/{action}/{*requestParams}/",
				"home/index/session/login",
				"home",
				"index",
				new Dictionary<string, string>()
				{
					{ "requestParams", "session/login"}
				},
				false),
                                // !!!
                                new TestItem("{controller}/{action}/{*requestParams}/login",
				"home/index/session/login",
				"home",
				"index",
				new Dictionary<string, string>()
				{
					{ "requestParams", "session"}
				},
				true),
                                // !!!
                                ////////////////////////////////////////////////
                        };

			// !!!
			TestWithItems(testItems);
			// !!!
		}

		[TestMethod]
		public void RouteTemplatesMatch_04_Test()
		{
			var testItems = new TestItem[]
			{
				new TestItem("background/{controller}/{action}/{*userId=0}",
				"background/session/login/1234",
				"session",
				"login",
				new Dictionary<string, string>()
				{
					{ "userId", "1234"}
				},
				true),
				new TestItem("frontpage/{controller}/{action}/{*userId=0}",
				"frontpage/session/login/1234",
				"session",
				"login",
				new Dictionary<string, string>()
				{
					{ "userId", "1234"}
				},
				true),
				new TestItem("background/{controller}/{action}/{*userId=0}",
				"frontpage/session/login/1234",
				"session",
				"login",
				new Dictionary<string, string>()
				{
					{ "userId", "1234"}
				},
				false),
				new TestItem("frontpage/{controller}/{action}/{*userId=0}",
				"background/session/login/1234/",
				"session",
				"login",
				new Dictionary<string, string>()
				{
					{ "userId", "1234"}
				},
				false),
			};

			// !!!
			TestWithItems(testItems);
			// !!!
		}
	}
}