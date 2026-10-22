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
// =========================================================
public sealed record OperationsValidatedSession(Guid UserId, Guid SessionId, string DisplayName, string PreferredLanguage, bool IsOwner, bool IsSetupSession, bool IsAdministrator = false);
