using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Boar.Red.Service.Extensions;

public static class ActionExecutingContextExtension
{
	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static object[] GetAttributeObjectsFromActionToController(
	    this ActionExecutingContext actionExecutingContext,
	    Type attributeType,
	    bool isNeedInheritAttributes = false)
	{
		var attributeObjects = Array.Empty<object>();
		if (actionExecutingContext.ActionDescriptor
		    is ControllerActionDescriptor controllerActionDescriptor)
		{
			attributeObjects
			    = controllerActionDescriptor
			    .MethodInfo
			    .GetCustomAttributes(
			    attributeType,
			    isNeedInheritAttributes);
		}

		if (attributeObjects.Length < 1)
		{
			attributeObjects
			= actionExecutingContext
			.Controller
			.GetType()
			.GetCustomAttributes(
			    attributeType,
			    isNeedInheritAttributes);
		}

		return attributeObjects;
	}

	public static object[] GetAttributeObjectsFromActionToController<AttributeType>(
	    this ActionExecutingContext actionExecutingContext,
	    bool isNeedInheritAttributes = false)
	{
		return GetAttributeObjectsFromActionToController(
		    actionExecutingContext,
		    typeof(AttributeType),
		    isNeedInheritAttributes);
	}

	public static AttributeType[] GetAttributesFromActionToController<AttributeType>(
	    this ActionExecutingContext actionExecutingContext,
	    bool isNeedInheritAttributes = false)
	{
		var attributeOriginalObjects = GetAttributeObjectsFromActionToController(
		    actionExecutingContext,
		    typeof(AttributeType),
		    isNeedInheritAttributes);
		if (attributeOriginalObjects != null)
		{
			var attributeObjects = new AttributeType[attributeOriginalObjects.Length];
			var attributeObjectsCount = attributeOriginalObjects.Length;
			for (var attributeOriginalObjectIndex = 0;
			    attributeOriginalObjectIndex < attributeObjectsCount;
			    attributeOriginalObjectIndex++)
			{
				attributeObjects[attributeOriginalObjectIndex]
				    = (AttributeType)attributeOriginalObjects[attributeOriginalObjectIndex];
			}
			return attributeObjects;
		}
		return [];
	}

	#endregion
}
