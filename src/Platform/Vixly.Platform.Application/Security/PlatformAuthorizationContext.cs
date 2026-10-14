using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformAuthorizationContext.cs
//
// PURPOSE:
// The trusted identity of the Platform user on whose behalf a
// use case runs.
//
// RESPONSIBILITIES:
// - Identify the authenticated Platform user
// - Carry that user's fixed authority level
// - Carry the id of the signed-in session, for step-up checks
//
// IMPORTANT NOTES:
// - Built only from the authenticated principal by the host
//   (Api or Web). It is never deserialized from a request body,
//   route, query string or form (Global Standard S-2)
// - Platform-global: not bound to any tenant
// - Carries no permissions. Effective permissions are resolved
//   from persistence by the authorization service, so a stale
//   or forged context cannot widen access
// - Contains no HTTP, Blazor or presentation types
// =========================================================
public sealed record PlatformAuthorizationContext
{
    public PlatformAuthorizationContext(
        Guid platformUserId,
        PlatformUserAuthorityLevel authorityLevel,
        Guid? sessionId = null)
    {
        if (platformUserId == Guid.Empty)
        {
            throw new DomainException(PlatformSecurityErrors.UserIdRequired);
        }

        if (!Enum.IsDefined(authorityLevel))
        {
            throw new DomainException(PlatformSecurityErrors.AuthorityLevelInvalid);
        }

        PlatformUserId = platformUserId;
        AuthorityLevel = authorityLevel;
        SessionId = sessionId;
    }

    public Guid PlatformUserId { get; }

    public PlatformUserAuthorityLevel AuthorityLevel { get; }

    public Guid? SessionId { get; }
}
