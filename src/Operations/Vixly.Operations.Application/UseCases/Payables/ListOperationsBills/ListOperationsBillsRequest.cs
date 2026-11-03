using Vixly.Operations.Domain.Payables;

namespace Vixly.Operations.Application.UseCases.Payables.ListOperationsBills;

// =========================================================
// FILE: ListOperationsBillsRequest.cs
//
// PURPOSE:
// The company’s bills (Slice 8): what is owed, overdue and waiting for approval.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsBillsRequest
{
    // None: every bill.
    public OperationsBillStatus? Status { get; init; }
}
