namespace Vixly.Operations.Application.UseCases.Account.SetMyOperationsPin;

// =========================================================
// FILE: SetMyOperationsPinRequest.cs
//
// PURPOSE:
// Sets or changes the signed-in person's 4-digit PIN for licensed devices (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetMyOperationsPinRequest
{
    public string? CurrentPassword { get; init; }

    public string? Pin { get; init; }
}
