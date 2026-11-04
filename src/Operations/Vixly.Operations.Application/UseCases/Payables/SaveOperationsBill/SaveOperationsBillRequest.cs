using Vixly.Operations.Domain.Payables;

namespace Vixly.Operations.Application.UseCases.Payables.SaveOperationsBill;

// =========================================================
// FILE: SaveOperationsBillRequest.cs
//
// PURPOSE:
// Enters a bill, or corrects one not yet approved (Slice 8): who sent it, their number, what for, dates, amount, and its shares on dispatches or shipments.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsBillRequest
{
    public Guid? BillId { get; init; }

    public OperationsBillParty Party { get; init; }

    public Guid PartyId { get; init; }

    public string? Reference { get; init; }

    public OperationsBillCategory Category { get; init; }

    public DateOnly BilledOn { get; init; }

    public DateOnly DueOn { get; init; }

    public decimal Amount { get; init; }

    public string? Note { get; init; }

    // A dispatch or shipment number and the amount put on it.
    public IReadOnlyList<(string Number, decimal Amount)> Shares { get; init; } = Array.Empty<(string, decimal)>();
}
