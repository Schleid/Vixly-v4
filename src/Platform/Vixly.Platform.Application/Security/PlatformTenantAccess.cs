using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformTenantAccess.cs
//
// PURPOSE:
// Role and step-up rules for calls from Operations (Slice 9, D-K8).
//
// RESPONSIBILITIES:
// - Refuse a caller whose role is not allowed or whose second factor was not checked
// - Turn the role into and from its token text
//
// IMPORTANT NOTES:
// - Checked first in each use case, before any read
// - The role is refused before the step-up, so a staff user is never asked for a second factor
// =========================================================
public static class PlatformTenantAccess
{
    public static Error? Require(PlatformTenantAuthorizationContext context, bool stepUp, params PlatformTenantActorRole[] roles)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(roles);

        if (roles.Length > 0 && Array.IndexOf(roles, context.Role) < 0)
        {
            return PlatformCommonErrors.RoleNotAllowed;
        }

        return stepUp && !context.StepUp ? PlatformCommonErrors.StepUpRequired : null;
    }

    public static string RoleName(PlatformTenantActorRole role)
    {
        return role switch
        {
            PlatformTenantActorRole.Owner => "owner",
            PlatformTenantActorRole.Admin => "admin",
            PlatformTenantActorRole.Billing => "billing",
            _ => "staff"
        };
    }

    public static PlatformTenantActorRole ParseRole(string? text)
    {
        return text?.Trim().ToLowerInvariant() switch
        {
            "owner" => PlatformTenantActorRole.Owner,
            "admin" => PlatformTenantActorRole.Admin,
            "billing" => PlatformTenantActorRole.Billing,
            _ => PlatformTenantActorRole.Staff
        };
    }
}
