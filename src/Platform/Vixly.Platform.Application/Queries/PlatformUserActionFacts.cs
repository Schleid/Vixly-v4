using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformUserActionFacts.cs
//
// PURPOSE:
// The few facts about a staff user that decide which
// administrator actions apply to them.
//
// RESPONSIBILITIES:
// - Carry status, authority, second-factor presence and the
//   number of open sessions
//
// IMPORTANT NOTES:
// - A projection: never a tracked entity (P-1)
// =========================================================
public sealed record PlatformUserActionFacts(
    Guid PlatformUserId,
    PlatformUserAccountStatus Status,
    PlatformUserAuthorityLevel AuthorityLevel,
    bool HasSecondFactor,
    int OpenSessionCount);
