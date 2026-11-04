namespace Vixly.Operations.Application.UseCases.Receiving.CancelMyOperationsPreAlert;

// =========================================================
// FILE: CancelMyOperationsPreAlertRequest.cs
//
// PURPOSE:
// A customer withdraws a pre-alert that has not been received (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CancelMyOperationsPreAlertRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid PreAlertId { get; init; }
}
