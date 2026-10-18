using BaoXia.Utils;
using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService;
using Boar.Red.Service.BrService.Models;
using Boar.Red.Service.WebService.Models;
using System.Reflection;

namespace Boar.Red.Service.Utils;

public class ServiceConsoleUtil
{
	////////////////////////////////////////////////
	// @静态常量
	////////////////////////////////////////////////

	#region 静态常量

	const string ServiceBootInfoHeader = "┌────────────────────────────────────────────────────────┐ ";
	const string ServiceBootInfoTextPrefix = "│ ";
	const string ServiceBootInfoContentSpaces = "                                                      ";
	const string ServiceBootInfoTextSuffix = " │ ";
	const string ServiceBootInfoFooter = "└────────────────────────────────────────────────────────┘ ";

	#endregion

	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	private static void WriteServiceInfoLine(
	    string? serviceBootInfoText,
	    ConsoleColor consoleBackgroundColor = ConsoleColor.Blue,
	    ConsoleColor consoleForegroundColor = ConsoleColor.Cyan,
	    string lineIntent = " ")
	{
		var serviceBootInfoTextLines = new List<string>();
		if (serviceBootInfoText == null
		    || serviceBootInfoText.Length < 1)
		{
			serviceBootInfoTextLines.Add(string.Empty);
		}
		else if (serviceBootInfoText == ServiceBootInfoHeader
		    || serviceBootInfoText == ServiceBootInfoFooter)
		{
			serviceBootInfoTextLines.Add(serviceBootInfoText);
		}
		else
		{
			serviceBootInfoText = serviceBootInfoText
			    .Replace("\r\n", "\r")
			    .Replace("\n", "\r");
			while (serviceBootInfoText.Length > 0)
			{
				var serviceBootInfoTextConsoleLength = 0;
				for (var serviceBootInfoTextCharIndex = 0;
				    serviceBootInfoTextCharIndex < serviceBootInfoText.Length;
				    serviceBootInfoTextCharIndex++)
				{
					var serviceBootInfoTextChar = serviceBootInfoText[serviceBootInfoTextCharIndex];
					if (serviceBootInfoTextChar == '\r')
					{
						var serviceBootInfoTextLine = serviceBootInfoText[..serviceBootInfoTextCharIndex];
						// !!!
						serviceBootInfoTextLines.Add(serviceBootInfoTextLine);
						// !!!
						serviceBootInfoText = serviceBootInfoText[(serviceBootInfoTextCharIndex + 1)..];
					}
					else if ((serviceBootInfoTextCharIndex + 1) == serviceBootInfoText.Length)
					{
						// !!!
						serviceBootInfoTextLines.Add(serviceBootInfoText);
						// !!!
						serviceBootInfoText = serviceBootInfoText[serviceBootInfoText.Length..];
					}
					else
					{
						var serviceBootInfoTextCharConsoleLength
						     = Char.IsAscii(serviceBootInfoTextChar)
						     ? 1
						     : 2;
						if ((serviceBootInfoTextConsoleLength + serviceBootInfoTextCharConsoleLength)
						    > ServiceBootInfoContentSpaces.Length)
						{
							var serviceBootInfoTextLine = serviceBootInfoText[..serviceBootInfoTextCharIndex];
							// !!!
							serviceBootInfoTextLines.Add(serviceBootInfoTextLine);
							// !!!
							serviceBootInfoText = serviceBootInfoText[serviceBootInfoTextCharIndex..];
						}
						else
						{
							serviceBootInfoTextConsoleLength += serviceBootInfoTextCharConsoleLength;
						}
					}
				}
			}
		}

		lock (ConsoleUtil.ConsoleWriteLocker)
		{
			foreach (var serviceBootInfoTextLine in serviceBootInfoTextLines)
			{
				var finalServiceBootInfoTextLine = serviceBootInfoTextLine;
				var finalServiceBootInfoTextLineUnasciiCharsCount = 0;
				foreach (var finalServiceBootInfoTextChar
				    in
				    finalServiceBootInfoTextLine)
				{
					if (!Char.IsAscii(finalServiceBootInfoTextChar))
					{
						finalServiceBootInfoTextLineUnasciiCharsCount++;
					}
				}
				while ((finalServiceBootInfoTextLine.Length + finalServiceBootInfoTextLineUnasciiCharsCount)
				    < ServiceBootInfoContentSpaces.Length)
				{
					finalServiceBootInfoTextLine += " ";
				}

				Console.Write("\r\n");
				Console.ResetColor();
				Console.Write(lineIntent);
				//
				Console.BackgroundColor = consoleBackgroundColor;
				Console.ForegroundColor = consoleForegroundColor;
				if (finalServiceBootInfoTextLine != ServiceBootInfoHeader
				    && finalServiceBootInfoTextLine != ServiceBootInfoFooter)
				{
					finalServiceBootInfoTextLine
					    = ServiceBootInfoTextPrefix
					    + finalServiceBootInfoTextLine
					    + ServiceBootInfoTextSuffix;
				}
				Console.Write(finalServiceBootInfoTextLine);
				Console.ResetColor();
				//
			}
		}
	}

	/// <summary>
	/// 使用指定的背景，前景颜色，缩进，在控制台显示服务信息。
	/// </summary>
	/// <param name="consoleBackgroundColor">指定的控制台，背景颜色，传“null”时，表示使用默认值（ConsoleColor.Blue）。</param>
	/// <param name="consoleForegroundColor">指定的控制台，前景颜色，传“null”时，表示使用默认值（ConsoleColor.Cyan）。</param>
	/// <param name="lineIntent">指定的控制台，缩进字符，传“null”时，表示使用默认值“ ”。</param>
	/// <param name="serviceInfes">要显示的，服务信息数组。</param>
	public static void WriteServiceInfes(
	    ConsoleColor? consoleBackgroundColor,
	    ConsoleColor? consoleForegroundColor,
	    string? lineIntent,
	    params string[] serviceInfes)
	{
		consoleBackgroundColor ??= ConsoleColor.Blue;
		consoleForegroundColor ??= ConsoleColor.Cyan;
		lineIntent ??= " ";

		serviceInfes
		    = serviceInfes.ArrayByInsertAt(
		    0,
		    ServiceBootInfoHeader);
		serviceInfes
		    = serviceInfes.ArrayByAdd(
		    ServiceBootInfoFooter);

		lock (ConsoleUtil.ConsoleWriteLocker)
		{
			foreach (var serviceBootInfo in serviceInfes)
			{
				WriteServiceInfoLine(
				    serviceBootInfo,
				    consoleBackgroundColor.Value,
				    consoleForegroundColor.Value,
				    lineIntent);
			}
			Console.WriteLine();
			Console.WriteLine();
		}
	}

	public static void WriteServiceInfes(
	    params string[] serviceInfes)
	{
		WriteServiceInfes(
		    ConsoleColor.Blue,
		    ConsoleColor.Cyan,
		    " ",
		    serviceInfes);
	}


	public static void WriteServiceInfoWithWebServiceConfig(WebServiceConfig? webServiceConfig,
	     ServiceConfig? bxServiceConfig, ServiceDiscoverer? serviceDescriptor)
	{
		int currentServiceHttpPort = 0;
		int currentServiceHttpsPort = 0;
		if (webServiceConfig != null)
		{
			currentServiceHttpPort = webServiceConfig.HttpPort;
			currentServiceHttpsPort = webServiceConfig.HttpsPort;
		}

		string? currentServiceName = null;
		string? currentServiceGroupName = null;
		string? currentServiceNodeName = null;
		string? currentServiceDescription = null;
		string? serviceManagerDiscoveryAddress = null;
		int serviceManagerDiscoveryListenPort = 0;
		string serviceDiscovererState = "启用";
		string serviceDiscovererDiscoverInfoReceiveState = "不接收";
		int serviceDiscovererListenPort = 0;
		string serviceDiscovererListenPortType = "（固定）";
		if (bxServiceConfig != null)
		{
			if (bxServiceConfig.ServiceInfo
			    is ServiceBaseInfo serviceInfo)
			{
				currentServiceName = serviceInfo.ServiceName;
				currentServiceGroupName = serviceInfo.GroupName;
				currentServiceNodeName = serviceInfo.NodeName;
				currentServiceDescription = serviceInfo.ServiceDescription;

			}
			if (bxServiceConfig.ServiceDiscovererConfig
			    is ServiceDiscovererConfig serviceDiscovererConfig)
			{
				serviceManagerDiscoveryAddress
				    = serviceDiscovererConfig.ServiceManagerAddress;
				serviceManagerDiscoveryListenPort
				    = serviceDiscovererConfig.ServiceManagerDiscoveryListenPort;
				serviceDiscovererListenPort
				    = serviceDiscovererConfig.ListenPort;
			}
		}
		if (string.IsNullOrEmpty(serviceManagerDiscoveryAddress))
		{
			serviceManagerDiscoveryAddress = "未设置";
		}

		if (serviceDescriptor != null)
		{
			if (serviceDescriptor.IsDisable)
			{
				serviceDiscovererState = "禁用";
			}
			if (serviceDescriptor.IsDiscoverInfoReceiveEnable)
			{
				serviceDiscovererDiscoverInfoReceiveState = "接收";
			}
			if (serviceDescriptor.IsListenPortAutoIncrease)
			{
				serviceDiscovererListenPortType = "（自动）";
			}
		}
		else
		{
			serviceDiscovererListenPortType = "未配置";
		}

		if (string.IsNullOrEmpty(currentServiceName))
		{
			currentServiceName = "默认服务";
		}
		if (string.IsNullOrEmpty(currentServiceDescription))
		{
			currentServiceDescription = "无描述信息。";
		}

		var bxServiceVersion
		    = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
		    ?? "未知版本";
		var currentServiceVersion
		    = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
		    ?? "未知版本";

		ServiceConsoleUtil.WriteServiceInfes(
		    string.Empty,
		    currentServiceName,
		    "v" + currentServiceVersion,
		    "服务描述：" + currentServiceDescription,
		    string.Empty,
		    string.Empty,
		    "Boar.Red 微服务（微服务架构） ",
		    "v" + bxServiceVersion,
		    "https://boar.red",
		    string.Empty,
		    string.Empty,
		    "服务分组：" + currentServiceGroupName,
		    "节点名称：" + currentServiceNodeName,
		    "Http端口：" + currentServiceHttpPort,
		    "Https端口：" + currentServiceHttpsPort,
		    "启动时间：" + DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss"),
		    string.Empty,
		    "服务管理器地址：" + serviceManagerDiscoveryAddress,
		    "服务管理器端口：" + serviceManagerDiscoveryListenPort,
		    string.Empty,
		    "服务发现器状态：" + serviceDiscovererState,
		    "服务的发现信息：" + serviceDiscovererDiscoverInfoReceiveState,
		    "服务发现器端口：" + serviceDiscovererListenPort + serviceDiscovererListenPortType);
	}

	#endregion
}
