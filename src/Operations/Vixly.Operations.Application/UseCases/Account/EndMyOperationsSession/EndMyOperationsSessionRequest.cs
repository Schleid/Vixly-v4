namespace Vixly.Operations.Application.UseCases.Account.EndMyOperationsSession;

// =========================================================
// FILE: EndMyOperationsSessionRequest.cs
//
// PURPOSE:
// Ends one of my sessions, for example on a lost phone (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class EndMyOperationsSessionRequest
{
    public Guid SessionId { get; init; }
}
