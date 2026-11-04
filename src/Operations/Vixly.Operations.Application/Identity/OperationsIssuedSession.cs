namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsIssuedSession.cs
//
// PURPOSE:
// A session just started: its id and the token for the cookie.
//
// RESPONSIBILITIES:
// - Carry them
//
// IMPORTANT NOTES:
// - The token goes to the cookie only
// =========================================================
public sealed record OperationsIssuedSession(Guid SessionId, string Token, DateTimeOffset ExpiresUtc);
