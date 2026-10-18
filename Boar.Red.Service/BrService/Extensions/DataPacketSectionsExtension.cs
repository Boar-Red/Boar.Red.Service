using BaoXia.Utils.Extensions;
using Boar.Red.Service.BrService.Models;

namespace Boar.Red.Service.BrService.Extensions;

public static class DataPacketSectionsExtension
{
	public static DataPacketSection? GetBytesDataSectionWithName(
	    this IEnumerable<DataPacketSection> dataPacketSections,
	    string? dataPacketSectionName)
	{
		if (string.IsNullOrEmpty(dataPacketSectionName))
		{
			return null;
		}

		foreach (var dataPacketSection in dataPacketSections)
		{
			if (dataPacketSectionName.EqualsIgnoreCase(dataPacketSection.Name)
			    && dataPacketSection.Type == DataPacketType.Bytes)
			{
				return dataPacketSection;
			}
		}
		return null;
	}
}
