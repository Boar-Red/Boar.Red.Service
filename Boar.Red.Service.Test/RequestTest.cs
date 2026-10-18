using BaoXia.Constants;
using Boar.Red.Service.ViewModels;

namespace Boar.Red.Service.Test;

[TestClass]
public class RequestTest
{
	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	[TestMethod]
	public void RequestWithUserTokenTest()
	{
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
		var request = new RequestWithUserToken(string.Empty, null);
#pragma warning restore CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
		var errorResponse = request.GetErrorResonseByCheckRequest();
		//{
		//	Assert.IsTrue(errorResponse?.Error.Equals(Error.InvalidRequest) == true);
		//	Assert.IsTrue(errorResponse?.ErrorDescription?.Equals("请求无效，客户端Ip信息的“IpAddressChain”为“空”。") == true);
		//}
		{
			Assert.IsTrue(errorResponse?.Error.Equals(Error.UserTokenNotExisted));
			Assert.IsTrue(errorResponse?.ErrorDescription?.Equals("请求无效，用户令牌为“空”。"));
		}
	}

	#endregion
}
