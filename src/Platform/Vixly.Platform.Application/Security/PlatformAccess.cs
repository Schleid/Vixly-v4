using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformAccess.cs
//
// PURPOSE:
// Answers what a permission snapshot allows, for use cases that
// need more than a yes or no from the gate.
//
// RESPONSIBILITIES:
// - Say whether the actor holds one permission
// - Say whether the actor holds every permission in a set
//
// IMPORTANT NOTES:
// - An inactive account holds nothing; a SuperUser holds
//   everything (same rules as PlatformAuthorizationService)
// - Used to offer actions in the UI and to stop privilege
//   escalation; the gate still guards every use case
// =========================================================
public static class PlatformAccess
{
    public static bool Holds(PlatformPermissionSnapshot? actor, string permissionCode)
    {
        return actor is { IsAccountActive: true }
            && (actor.AuthorityLevel == PlatformUserAuthorityLevel.SuperUser || actor.PermissionCodes.Contains(permissionCode));
    }

    public static bool Covers(PlatformPermissionSnapshot? actor, IEnumerable<string> permissionCodes)
    {
        ArgumentNullException.ThrowIfNull(permissionCodes);

        return actor is { IsAccountActive: true }
            && (actor.AuthorityLevel == PlatformUserAuthorityLevel.SuperUser || permissionCodes.All(actor.PermissionCodes.Contains));
    }
}
