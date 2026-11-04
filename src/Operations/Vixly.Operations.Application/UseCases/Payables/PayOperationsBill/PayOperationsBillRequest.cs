using Vixly.Operations.Domain.Payables;

namespace Vixly.Operations.Application.UseCases.Payables.PayOperationsBill;

// =========================================================
// FILE: PayOperationsBillRequest.cs
//
// PURPOSE:
// Records a payment the company made on an approved bill (Slice 8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PayOperationsBillRequest
{
    public Guid BillId { get; init; }

    public DateOnly PaidOn { get; init; }

    public decimal Amount { get; init; }

    public OperationsBillPaymentMethod Method { get; init; }

    public string? Reference { get; init; }
}
