namespace Vixly.Platform.Application.UseCases.MyAccount.EndMySessions;

// =========================================================
// FILE: EndMySessionsRequest.cs
//
// PURPOSE:
// Input for EndMySessions.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Empty SessionId ends every session except this one
// - The session in use is ended by signing out, not here
// =========================================================
public sealed class EndMySessionsRequest
{
    public Guid? SessionId { get; init; }
}
