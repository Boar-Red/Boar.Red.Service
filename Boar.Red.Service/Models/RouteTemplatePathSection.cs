using BaoXia.Utils.Extensions;
using Boar.Red.Service.Constants;

namespace Boar.Red.Service.Models
{
	public class RouteTemplatePathSection
	{

		////////////////////////////////////////////////
		// @类方法
		////////////////////////////////////////////////

		#region 类方法

		public static void UpdatePathSectionParamInfo(
		    RouteTemplatePathSection pathSectionObject)
		{
			var pathSection = pathSectionObject.PathSection;

			bool isCatchAllPath = false;
			string? paramName = null;
			string? paramValueDefault = null;
			bool isOptionalParam = false;
			////////////////////////////////////////////////
			if (pathSection.StartsWith(RouteTemplateConstants.RouteParamSymbol_DeclareSectionBegin)
			    && pathSection.EndsWith(RouteTemplateConstants.RouteParamSymbol_DeclareSectionEnd))
			{
				var paramInfoString = pathSection.Trim(
				    [
	    RouteTemplateConstants.RouteParamSymbol_DeclareSectionBegin,
	    RouteTemplateConstants.RouteParamSymbol_DeclareSectionEnd
				    ]);
				if (string.IsNullOrEmpty(paramInfoString))
				{
					throw new ArgumentException("无法从路由模板中获取有效的参数信息。");
				}
				//var routeParamSymbol_DeclareSectionBeginString
				//        = new string(RouteTemplateConstants.RouteParamSymbol_DeclareSectionBegin.ToString());

				var paramNameLength
				    = CharExtension.GetStringCharsCountFromStringBeforeKeys(
				    [
		RouteTemplateConstants.RouteParamSymbol_DefaultValue,
		RouteTemplateConstants.RouteParamSymbol_DeclareSectionEnd
				    ],
				    true,
				    paramInfoString,
				    0,
				    true,
				    StringComparison.OrdinalIgnoreCase,
				    out paramName,
				    out var paramNameEndKey);
				if (string.IsNullOrEmpty(paramName))
				{
					throw new ArgumentException("无法从路由模板中获取有效的参数名称。");
				}
				if (paramName.StartsWith(RouteTemplateConstants.RouteParamSymbol_CatchAllPath))
				{
					isCatchAllPath = true;
					paramName = paramName.Trim(
					    [
		RouteTemplateConstants.RouteParamSymbol_CatchAllPath
					    ]);
				}
				if (paramName?.EndsWith(RouteTemplateConstants.RouteParamSymbol_Optional)
				    == true)
				{
					isOptionalParam = true;
					paramName = paramName.Trim(
					    [
		RouteTemplateConstants.RouteParamSymbol_Optional
					    ]);
				}

				if (RouteTemplateConstants.RouteParamSymbol_DefaultValue
				    .EqualsIgnoreCase(paramNameEndKey))
				{
					var routeParamValueBeginCharIndex
					    = 0
					    + paramNameLength
					    + RouteTemplateConstants.RouteParamSymbol_DefaultValue.Length;
					CharExtension.GetStringCharsCountFromStringBeforeKey(
					    RouteTemplateConstants.RouteParamSymbol_DeclareSectionEnd,
					    true,
					    paramInfoString,
					    routeParamValueBeginCharIndex,
					    true,
					    StringComparison.OrdinalIgnoreCase,
					    out paramValueDefault);
				}
			}
			////////////////////////////////////////////////
			pathSectionObject.IsCatchAllPath = isCatchAllPath;
			pathSectionObject.ParamName = paramName;
			if (paramName?.Length > 0)
			{
				pathSectionObject._isControllerNameSection
				    = RouteTemplateConstants.RouteParamSymbol_ControllerName
				    .EqualsIgnoreCase(paramName);
				if (!pathSectionObject._isControllerNameSection)
				{
					pathSectionObject._isActionNameSection
					    = RouteTemplateConstants.RouteParamSymbol_ActionName
					    .EqualsIgnoreCase(paramName);
				}
			}
			pathSectionObject.ParamValueDefault = paramValueDefault;
			pathSectionObject.IsOptionalParam = isOptionalParam;
		}

		#endregion


		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性

		protected string _pathSection = string.Empty;
		public string PathSection
		{
			get
			{
				return _pathSection;
			}
			set
			{
				if (_pathSection.EqualsIgnoreCase(value))
				{
					return;
				}

				_pathSection = value;
				// !!!
				UpdatePathSectionParamInfo(this);
				// !!!
			}
		}

		public bool IsCatchAllPath { get; set; }

		public string? ParamName { get; set; }

		public string? ParamValueDefault { get; set; }

		public bool IsOptionalParam { get; set; }

		public bool IsConstantPathSection
		{
			get
			{
				if (string.IsNullOrEmpty(ParamName))
				{
					return true;
				}
				return false;
			}
		}

		public bool IsDirectorySeparatorCharSection
		{
			get
			{
				if (IsConstantPathSection
				    && PathSection?.Length == 1
				    && PathSection[0] == Path.AltDirectorySeparatorChar)
				{
					return true;
				}
				return false;
			}
		}

		public bool IsParamSection
		{
			get
			{
				if (ParamName?.Length > 0)
				{
					return true;
				}
				return false;
			}
		}

		protected bool _isControllerNameSection;
		public bool IsControllerNameSection => _isControllerNameSection;

		protected bool _isActionNameSection;
		public bool IsActionNameSection => _isActionNameSection;

		#endregion


		////////////////////////////////////////////////
		// @自身实现
		////////////////////////////////////////////////

		#region 自身实现

		public RouteTemplatePathSection(string pathSection)
		{
			PathSection = pathSection;
		}

		#endregion
	}
}
