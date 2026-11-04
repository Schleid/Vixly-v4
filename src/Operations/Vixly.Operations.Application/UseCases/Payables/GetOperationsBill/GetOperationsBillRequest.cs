namespace Vixly.Operations.Application.UseCases.Payables.GetOperationsBill;

// =========================================================
// FILE: GetOperationsBillRequest.cs
//
// PURPOSE:
// One bill (Slice 8), or the empty form for a new one, with who can send bills and what the person may do with it.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsBillRequest
{
    // None: a new bill.
    public Guid? BillId { get; init; }
}
