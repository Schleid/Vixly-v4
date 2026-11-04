namespace Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterSupplyLine;

// =========================================================
// FILE: ChangeOperationsCounterSupplyLineRequest.cs
//
// PURPOSE:
// A line of supplies changed at the counter (Slice 5E, S-11, S-17 to S-19): its quantity (0 removes it) and its discount within the company's policy.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsCounterSupplyLineRequest
{
    public Guid SaleId { get; init; }

    public Guid LineId { get; init; }

    // 0: remove the line.
    public int Quantity { get; init; }

    public decimal DiscountPercent { get; init; }

    public string? DiscountReason { get; init; }

    // Above the clerk's limit: the email and authenticator code of a person who may approve discounts.
    public string? ApproverEmail { get; init; }

    public string? ApproverCode { get; init; }

    // The form's key: sent twice (a double click), applied once (ADR-0019).
    public Guid Key { get; init; }
}
