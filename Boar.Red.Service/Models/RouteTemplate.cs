using BaoXia.Utils.Extensions;
using Boar.Red.Service.Constants;

namespace Boar.Red.Service.Models
{
	public class RouteTemplate(
		    string name,
		    string templateString)
	{

		////////////////////////////////////////////////
		// @静态常量
		////////////////////////////////////////////////

		#region 静态常量

		public static readonly char AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar;

		#endregion



		////////////////////////////////////////////////
		// @类方法
		////////////////////////////////////////////////

		#region 类方法

		public static List<RouteTemplatePathSection> CreatePathSectionsWithTemplateString(string? templateString)
		{
			if (string.IsNullOrEmpty(templateString))
			{
				throw new ArgumentNullException(templateString, "无法创建空的路由模板路径块。");
			}

			////////////////////////////////////////////////
			// 1/3，解析出所有的路径块：
			////////////////////////////////////////////////

			if (templateString.StartsWith(AltDirectorySeparatorChar) != true)
			{
				templateString = AltDirectorySeparatorChar + templateString;
			}

			var pathSections = new List<RouteTemplatePathSection>();
			var charsCount = templateString.Length;
			RouteTemplatePathSection? controllerNamePathSection = null;
			RouteTemplatePathSection? actionNamePathSection = null;
			for (var charIndex = 0;
			    charIndex < charsCount;
			    charIndex++)
			{
				var constantSectionStringLength
				    = CharExtension.GetStringCharsCountFromStringBeforeKey(
				    RouteTemplateConstants.RouteParamSymbol_DeclareSectionBegin,
				    false,
				    templateString,
				    charIndex,
				    false,
				    StringComparison.OrdinalIgnoreCase,
				    out var constantSectionString);
				if (constantSectionStringLength == 0)
				{
					var paramDeclareSectionBeginCharIndex = charIndex;
					var paramDeclareSectionContentLength
					    = CharExtension.GetStringCharsCountFromStringBeforeKey(
					    RouteTemplateConstants.RouteParamSymbol_DeclareSectionEnd,
					    false,
					    templateString,
					    charIndex + 1,
					    false,
					    StringComparison.OrdinalIgnoreCase,
					    out var _);
					if (paramDeclareSectionContentLength < 0)
					{
						throw new ArgumentException("没有找到路由参数定义的结束符号“}”。", templateString);
					}

					var paramDeclareSectionLength
					    = RouteTemplateConstants.RouteParamSymbol_DeclareSectionBegin.Length
					    + paramDeclareSectionContentLength
					    + RouteTemplateConstants.RouteParamSymbol_DeclareSectionEnd.Length;
					var paramDeclareSectionString
					    = templateString.Substring(
					    paramDeclareSectionBeginCharIndex,
					    paramDeclareSectionLength);

					////////////////////////////////////////////////
					// !!!
					charIndex = paramDeclareSectionBeginCharIndex
					    + paramDeclareSectionLength
					    - 1;
					// !!!
					////////////////////////////////////////////////

					var paramDeclareSection = new RouteTemplatePathSection(paramDeclareSectionString);
					{
						if (paramDeclareSection.IsControllerNameSection)
						{
							if (controllerNamePathSection != null)
							{
								throw new ArgumentException("不能指定两个控制器名称模板路径块。", templateString);
							}
							controllerNamePathSection = paramDeclareSection;
						}
						else if (paramDeclareSection.IsActionNameSection)
						{
							if (actionNamePathSection != null)
							{
								throw new ArgumentException("不能指定两个执行器名称模板路径块。", templateString);
							}
							actionNamePathSection = paramDeclareSection;
						}
					}
					pathSections.Add(paramDeclareSection);
				}
				else
				{
					if (constantSectionStringLength < 0)
					{
						constantSectionStringLength = templateString.Length - charIndex;
						constantSectionString = templateString.Substring(charIndex, constantSectionStringLength);
					}

					if (constantSectionString?.Length > 0)
					{
						////////////////////////////////////////////////
						// !!!
						charIndex = charIndex
						    + constantSectionString.Length
						    - 1;
						// !!!
						////////////////////////////////////////////////

						var constantSection = new RouteTemplatePathSection(constantSectionString);
						{ }
						pathSections.Add(constantSection);
					}
				}
			}

			////////////////////////////////////////////////
			// 2/3，检查必须的控制器名称：
			////////////////////////////////////////////////
			if (controllerNamePathSection == null)
			{
				throw new ArgumentException("没有找到控制器名称模板路径块。", templateString);
			}
			if (actionNamePathSection == null)
			{
				throw new ArgumentException("没有找到执行器名称模板路径块。", templateString);
			}

			return pathSections;
		}

		#endregion



		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性

		public string Name { get; set; } = name;

		public string TemplateString { get; set; } = templateString;

		public List<RouteTemplatePathSection> PathSections { get; set; } = CreatePathSectionsWithTemplateString(templateString);

		#endregion



		////////////////////////////////////////////////
		// @自身实现
		////////////////////////////////////////////////

		#region 自身实现

		public RouteInfo? GetRouteInfoWithUrlPath(string? urlPath)
		{
			if (string.IsNullOrEmpty(urlPath))
			{
				return null;
			}
			if (urlPath.StartsWith(AltDirectorySeparatorChar) != true)
			{
				urlPath = AltDirectorySeparatorChar + urlPath;
			}

			var pathSections = PathSections;
			var pathSectionsCount = pathSections.Count;
			var urlPathMatchBeginIndex = 0;
			var urlPathLength = urlPath.Length;
			//var urlPathLastCharIndex = urlPath.Length - 1;
			var isPathMatched = true;

			string? controllerName = null;
			string? actionName = null;
			Dictionary<string, string>? actionParams = null;
			int reservedActionParamsCount = 0;
			var toGetNextConstantPathSectionFrom
			    = new Func<List<RouteTemplatePathSection>, int, RouteTemplatePathSection?>(
				(pathSections, beginPathSectionIndex) =>
				{
					var pathSectionsCount = pathSections.Count;
					for (var pathSectionIndex = beginPathSectionIndex; pathSectionIndex < pathSectionsCount; pathSectionIndex++)
					{
						var pathSection = pathSections[pathSectionIndex];
						if (pathSection.IsConstantPathSection)
						{
							return pathSection;
						}
					}
					return null;
				});
			string? altDirectorySeparatorString = null;

			for (var pathSectionIndex = 0;
			    pathSectionIndex < pathSectionsCount;
			    pathSectionIndex++)
			{
				var currentPathSection = pathSections[pathSectionIndex];
				if (currentPathSection.IsConstantPathSection)
				{
					if (CharExtension.IsCharsOfStringEqualsKey(
					    currentPathSection.PathSection,
					    urlPath,
					    urlPathMatchBeginIndex,
					    out var charsCountEqualed,
					    StringComparison.OrdinalIgnoreCase))
					{
						urlPathMatchBeginIndex += charsCountEqualed;
					}
					else if (currentPathSection.IsDirectorySeparatorCharSection
					    && urlPathMatchBeginIndex == urlPathLength)
					{
						// !!!
						// urlPathMatchBeginIndex += 0;
						// !!!
					}
					else
					{
						// !!!
						isPathMatched = false;
						break;
						// !!!
					}
				}
				else
				{
					int currentPathSectionCharsCount;
					string? currentPathSectionChars;
					var nextConstantPathSection
					    = toGetNextConstantPathSectionFrom(
					    pathSections,
					    pathSectionIndex + 1);
					if (nextConstantPathSection != null)
					{
						currentPathSectionCharsCount
						    = CharExtension.GetStringCharsCountFromStringBeforeKey(
						    nextConstantPathSection.PathSection,
						    false,
						    urlPath,
						    urlPathMatchBeginIndex,
						    false,
						    StringComparison.OrdinalIgnoreCase,
						    out currentPathSectionChars);
						if (currentPathSectionCharsCount < 0)
						{
							if (nextConstantPathSection.IsDirectorySeparatorCharSection)
							{
								currentPathSectionCharsCount
								    = urlPath.Length
								    - urlPathMatchBeginIndex;
								currentPathSectionChars
								    = urlPath.Substring(
								    urlPathMatchBeginIndex,
								    currentPathSectionCharsCount);
							}
							else
							{
								// !!!
								isPathMatched = false;
								break;
								// !!!
							}
						}
					}
					else if (currentPathSection.IsCatchAllPath)
					{
						currentPathSectionCharsCount
						    = urlPath.Length - urlPathMatchBeginIndex;
						currentPathSectionChars
						    = urlPath.Substring(
						    urlPathMatchBeginIndex,
						    currentPathSectionCharsCount);
					}
					else
					{
						altDirectorySeparatorString ??= AltDirectorySeparatorChar.ToString();
						currentPathSectionCharsCount
						    = CharExtension.GetStringCharsCountFromStringBeforeKey(
						    altDirectorySeparatorString,
						    true,
						    urlPath,
						    urlPathMatchBeginIndex,
						    false,
						    StringComparison.OrdinalIgnoreCase,
						    out currentPathSectionChars);
					}


					////////////////////////////////////////////////
					var routeParamName = currentPathSection.ParamName;
					string? routeParamValue = null;
					if (currentPathSectionCharsCount > 0)
					{
						// !!!
						urlPathMatchBeginIndex += currentPathSectionCharsCount;
						//
						routeParamValue = currentPathSectionChars;
						// !!!
					}


					if (currentPathSection.IsOptionalParam == true
					    || !string.IsNullOrEmpty(routeParamValue))
					{
						if (routeParamName?.Length > 0)
						{
							if (string.IsNullOrEmpty(currentPathSectionChars))
							{
								routeParamValue = currentPathSection.ParamValueDefault;
							}

							if (RouteTemplateConstants
							    .RouteParamSymbol_ControllerName.EqualsIgnoreCase(
							    routeParamName))
							{
								// !!!
								controllerName = routeParamValue;
								reservedActionParamsCount++;
								// !!!
							}
							else if (RouteTemplateConstants
							    .RouteParamSymbol_ActionName.EqualsIgnoreCase(
							    routeParamName))
							{
								// !!!
								actionName = routeParamValue;
								reservedActionParamsCount++;
								// !!!
							}


							actionParams ??= [];
							actionParams!.AddOrSet(routeParamName, routeParamValue);
						}
					}
					else
					{
						// !!!
						isPathMatched = false;
						break;
						// !!!
					}
					////////////////////////////////////////////////
				}
			}
			if (isPathMatched
			    && urlPathMatchBeginIndex + 1 == urlPathLength
			    && urlPath[urlPathMatchBeginIndex] == AltDirectorySeparatorChar)
			{
				isPathMatched = true;
			}
			else if (urlPathMatchBeginIndex < urlPathLength)
			{
				isPathMatched = false;
			}

			if (isPathMatched != true
			    || string.IsNullOrEmpty(controllerName)
			    || string.IsNullOrEmpty(actionName))
			{
				return null;
			}

			var routeInfo = new RouteInfo(
			    controllerName,
			    actionName,
			    actionParams,
			    reservedActionParamsCount);
			{ }
			return routeInfo;
		}

		#endregion
	}
}
