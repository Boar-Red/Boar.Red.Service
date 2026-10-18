using System.Reflection;

namespace Boar.Red.Service.BrService;

public static class BrServiceTypesRegistered
{
	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

	public static Type[] ServiceTypes { get; internal set; } = [];

	public static Type[] IWebSocketServiceTypes { get; internal set; } = [];

	#endregion


	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	// ReSharper disable once UnusedMember.Global
	public static Type? GetServiceTypesWithType(Type objectserviceType)
	{
		var allServiceTypes = ServiceTypes;
		foreach (var serviceType in allServiceTypes)
		{
			if (serviceType.Equals(objectserviceType))
			{
				return serviceType;
			}
		}
		return null;
	}

	// ReSharper disable once UnusedMember.Global
	public static List<Type>? GetServiceTypesWithBaseClass(Type baseType)
	{
		List<Type>? serviceTypes = null;
		var allServiceTypes = ServiceTypes;
		foreach (var serviceType in allServiceTypes)
		{
			if (!serviceType.IsSubclassOf(baseType))
			{
				continue;
			}

			//
			serviceTypes ??= [];
			serviceTypes.Add(serviceType);
			//
		}

		return serviceTypes;
	}

	// ReSharper disable once UnusedMember.Global
	public static List<Type>? GetServiceTypesWithInterface(Type interfaceType)
	{
		List<Type>? serviceTypes = null;
		var allServiceTypes = ServiceTypes;
		foreach (var serviceType in allServiceTypes)
		{
			if (!interfaceType.IsAssignableFrom(serviceType))
			{
				continue;
			}

			//
			serviceTypes ??= [];
			serviceTypes.Add(serviceType);
			//
		}

		return serviceTypes;
	}

	// ReSharper disable once UnusedMember.Global
	public static List<Type>? GetServiceTypesWithAttribute<TAttribute>()
	    where TAttribute : Attribute
	{
		List<Type>? serviceTypes = null;
		var allServiceTypes = ServiceTypes;
		foreach (var serviceType in allServiceTypes)
		{
			if (serviceType.GetCustomAttribute<TAttribute>() == null)
			{
				continue;
			}

			//
			serviceTypes ??= [];
			serviceTypes.Add(serviceType);
			//
		}

		return serviceTypes;
	}

	// ReSharper disable once UnusedMember.Global
	public static List<Type>? GetServiceTypesWithAttribute(Type attributeType)
	{
		List<Type>? serviceTypes = null;
		var allServiceTypes = ServiceTypes;
		foreach (var serviceType in allServiceTypes)
		{
			if (serviceType.GetCustomAttribute(attributeType) == null)
			{
				continue;
			}

			//
			serviceTypes ??= [];
			serviceTypes.Add(serviceType);
			//
		}

		return serviceTypes;
	}

	#endregion
}