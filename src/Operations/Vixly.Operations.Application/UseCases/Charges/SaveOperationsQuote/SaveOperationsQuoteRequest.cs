using Vixly.Operations.Application.Charges;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsQuote;

// =========================================================
// FILE: SaveOperationsQuoteRequest.cs
//
// PURPOSE:
// Saves a quote for a customer on one service to a zone, with an optional preferred price (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsQuoteRequest
{
    public string? CustomerCode { get; init; }

    // Slice 4C: the route; no origin: the company's only origin, else any.
    public Guid? OriginWarehouseId { get; init; }

    public Guid? DestinationWarehouseId { get; init; }

    public string? ServiceCode { get; init; }

    public IReadOnlyList<OperationsPieceInput>? Pieces { get; init; }

    public bool Consolidated { get; init; }

    public bool Insured { get; init; }

    public OperationsDeliveryModes Delivery { get; init; } = OperationsDeliveryModes.None;

    public IReadOnlyDictionary<Guid, int>? ExtraQuantities { get; init; }

    public decimal? PreferredTotal { get; init; }

    public string? Reason { get; init; }

    public int ValidDays { get; init; } = OperationsQuote.DefaultValidDays;

    public string? CommandKey { get; init; }
}
