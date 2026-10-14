namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformUserAuthorityLevel.cs
//
// PURPOSE:
// The fixed security authority of a Platform user.
//
// RESPONSIBILITIES:
// - Separate system authority from configurable roles
// - Protect critical accounts from changes by lower levels
//
// IMPORTANT NOTES:
// - This is not a role. Roles and permissions are configurable
//   data; authority is fixed and ordered
// - Numeric values are stored and compared; never renumber
//   (Global Standard C-4 applies to persisted enums as well)
// =========================================================
public enum PlatformUserAuthorityLevel
{
    Standard = 0,
    Manager = 1,
    Administrator = 2,
    SuperUser = 3
}
