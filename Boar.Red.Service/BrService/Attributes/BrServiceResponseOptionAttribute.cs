using Boar.Red.Service.BrService.Constants;

namespace Boar.Red.Service.BrService.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class BrServiceResponseOptionAttribute(
    DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.None)
    : Attribute
{
	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	public DataPackageEncryptionMethod EncryptionMethod { get; set; } = encryptionMethod;

	#endregion
}