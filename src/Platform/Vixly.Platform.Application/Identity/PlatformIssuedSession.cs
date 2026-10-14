namespace Vixly.Platform.Application.Identity;

// =========================================================
// FILE: PlatformIssuedSession.cs
//
// PURPOSE:
// A new staff session, as handed to the host to set the
// session cookie.
//
// RESPONSIBILITIES:
// - Carry the raw token (for the cookie only), the session id
//   and the absolute expiry
//
// IMPORTANT NOTES:
// - The raw token is never stored or logged
// =========================================================
public sealed record PlatformIssuedSession(Guid SessionId, string Token, DateTimeOffset ExpiresUtc);
