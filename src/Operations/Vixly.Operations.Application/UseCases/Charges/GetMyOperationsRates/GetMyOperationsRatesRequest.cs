using Vixly.Operations.Application.Charges;

namespace Vixly.Operations.Application.UseCases.Charges.GetMyOperationsRates;

// =========================================================
// FILE: GetMyOperationsRatesRequest.cs
//
// PURPOSE:
// The customer's rate calculator to a zone and the estimate of their shipment, always an estimate (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsRatesRequest
{
    public string? SessionToken { get; init; }

    // Slice 4C: the route; no origin: the company's only origin, else any.
    public Guid? OriginWarehouseId { get; init; }

    public Guid? DestinationWarehouseId { get; init; }

    public bool HomeDelivery { get; init; }

    public IReadOnlyList<OperationsPieceInput>? Pieces { get; init; }

    public bool Consolidated { get; init; }

    public bool Insured { get; init; }

    public Guid? ShipmentId { get; init; }
}
