namespace Boar.Red.Service.Constants
{
	public class RouteTemplateConstants
	{
		////////////////////////////////////////////////
		// @静态常量
		////////////////////////////////////////////////

		#region 静态常量

		public const string RouteTemplate_NameDefault = "default";

		public const string RouteParamSymbol_DeclareSectionBegin = "{";
		public const string RouteParamSymbol_DeclareSectionEnd = "}";

		public const string RouteParamSymbol_CatchAllPath = "*";

		public const string RouteParamSymbol_Optional = "?";

		public const string RouteParamSymbol_DefaultValue = "=";

		public const string RouteParamSymbol_ControllerName = "Controller";

		public const string RouteParamSymbol_ActionName = "Action";

		/// <summary>
		/// 路由模板默认值。
		/// </summary>
		public static readonly Dictionary<string, string> RouteTemplatesDefault = new()
		{
			{ RouteTemplate_NameDefault, "{Controller}/{Action}" },
		};

		#endregion
	}
}
