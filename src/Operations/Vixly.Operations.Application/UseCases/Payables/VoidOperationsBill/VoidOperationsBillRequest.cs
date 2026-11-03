namespace Vixly.Operations.Application.UseCases.Payables.VoidOperationsBill;

// =========================================================
// FILE: VoidOperationsBillRequest.cs
//
// PURPOSE:
// Voids a bill entered by mistake, while nothing is paid on it (Slice 8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class VoidOperationsBillRequest
{
    public Guid BillId { get; init; }

    public string? Reason { get; init; }
}
