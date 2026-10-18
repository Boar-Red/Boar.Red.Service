namespace Boar.Red.Service.Constants;

public enum TokenCheckResult
{
	Unknown = 0,

	ValidToken = 1,

	InvalidTokenByInvalidTokenValue = -1,

	InvalidTokenByNotFound = -2,

	InvalidTokenBySignedInvalid = -3,

	InvalidTokenByExpired = -4,

	InvalidTokenByInvalidUserId = -5,

	InvalidTokenByNeedPermission = -6,

	InvalidTokenByOtherReason = -99,

	InvalidTokenSourceStateByUnknow = -100,

	InvalidTokenSourceStateByRealPersonIdentityVerifiedNecessary = -101,

	InvalidTokenSourceStateByUserBanned = -102,

	InvalidTokenSourceStateByUserDeleted = -103,

	InvalidTokenSourceStateByOtherReason = -199
}
