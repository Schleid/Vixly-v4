using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Identity;

// =========================================================
// FILE: PlatformSessionFacts.cs
//
// PURPOSE:
// What the session check needs to know about the staff member (harmonization H-10, owner 2026-11-04).
//
// RESPONSIBILITIES:
// - Carry whether the account may act, its authority level, the name and the language
//
// IMPORTANT NOTES:
// - Read on every request and every minute of an open page: a small read, never the whole staff member
// =========================================================
public sealed record PlatformSessionFacts(
    Guid PlatformUserId,
    bool IsActive,
    PlatformUserAuthorityLevel AuthorityLevel,
    string FirstName,
    string LastName,
    string PreferredLanguage);
