namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformTenantAuthorizationContext.cs
//
// PURPOSE:
// Who calls Platform from Operations for one tenant: the tenant, the acting tenant user and what Operations checked (Slices 7 and 9).
//
// RESPONSIBILITIES:
// - Carry the tenant the call is for and the Operations user reference and display name
// - Carry the user's role, whether a second factor was checked, the calling plane and the browser IP and user agent
//
// IMPORTANT NOTES:
// - Built only from a valid service token, never from a route or body (S-2, S-13)
// - Use cases check the role and step-up they need with PlatformTenantAccess
// =========================================================
public sealed record PlatformTenantAuthorizationContext(
    Guid PlatformTenantId,
    string? ActorReference,
    string? ActorName,
    PlatformTenantActorRole Role = PlatformTenantActorRole.Staff,
    bool StepUp = false,
    string? Caller = null,
    string? ClientIp = null,
    string? UserAgent = null);
