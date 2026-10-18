using System.Text.Json.Serialization;

namespace Boar.Red.Service.BrService.Models;

public class ApplicationInfo
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public string ApplicationIdentity { get; set; } = default!;

	public string Password { get; set; } = default!;

	public string AesKey { get; set; } = default!;

	public int ConnectionsCountMax { get; set; }

	public DateTime ExpireTime { get; set; }


	[JsonIgnore]
	public bool IsValid
	{
		get
		{
			if (this.ApplicationIdentity?.Length > 0)
			{
				return true;
			}
			return false;
		}
	}

	#endregion
}
