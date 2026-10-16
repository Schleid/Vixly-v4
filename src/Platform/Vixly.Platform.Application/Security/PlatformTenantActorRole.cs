namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformTenantActorRole.cs
//
// PURPOSE:
// The role an Operations user holds in their tenant, as sent in the service token (Slice 9, D-K8).
//
// RESPONSIBILITIES:
// - Name the roles Platform rules can ask for
//
// IMPORTANT NOTES:
// - Staff is the lowest role and the default; a missing or unknown role is Staff
// - Stored as the token text: owner, admin, billing, staff
// =========================================================
public enum PlatformTenantActorRole
{
    Staff = 0,
    Owner = 1,
    Admin = 2,
    Billing = 3
}
