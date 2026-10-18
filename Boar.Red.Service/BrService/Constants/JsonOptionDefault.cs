using System.Text.Json;

namespace Boar.Red.Service.BrService.Constants
{
	public class JsonOptionDefault
	{
		////////////////////////////////////////////////
		// @静态变量
		////////////////////////////////////////////////

		#region 静态变量

		public static readonly JsonDocumentOptions JsonDocumentOptions = new()
		{
			AllowTrailingCommas = true,
		};

		#endregion
	}
}
