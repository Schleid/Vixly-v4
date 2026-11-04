namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsValidatedSession.cs
//
// PURPOSE:
// A session that passed its checks on this request.
//
// RESPONSIBILITIES:
// - Carry who is signed in
//
// IMPORTANT NOTES:
// - Built only by OperationsSessionValidator
// - IdleTimes: the company's inactivity times, for the page's warning and sign-out (owner, 2026-10-27)
// =========================================================
public sealed record OperationsValidatedSession(Guid UserId, Guid SessionId, string DisplayName, string PreferredLanguage, bool IsOwner, bool IsSetupSession, bool IsAdministrator = false)
{
    public OperationsIdleTimes IdleTimes { get; init; } = OperationsIdleTimes.Default;
}
