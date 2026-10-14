using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.Identity;

// =========================================================
// FILE: PlatformValidatedSession.cs
//
// PURPOSE:
// A valid session with the facts the host shows: the
// authorization context, the display name and the language.
//
// RESPONSIBILITIES:
// - Carry what the host puts into the signed-in principal
//
// IMPORTANT NOTES:
// - Built from the database on every request
// =========================================================
public sealed record PlatformValidatedSession(
    PlatformAuthorizationContext Context,
    string DisplayName,
    string PreferredLanguage);
