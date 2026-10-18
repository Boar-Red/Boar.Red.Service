namespace Boar.Red.Service.Models;

public class ServiceAdministrator
{
	public string? Account { get; set; }

	public string? Password { get; set; }

	public string? EMail { get; set; }

	public DateTime ExpireTime { get; set; }

	public string? Remark { get; set; }

	public bool IsValid
	{
		get
		{
			if (Account?.Length > 0
			    && Password?.Length > 0
			    && ExpireTime >= DateTime.Now)
			{
				return true;
			}
			return false;
		}
	}
}
