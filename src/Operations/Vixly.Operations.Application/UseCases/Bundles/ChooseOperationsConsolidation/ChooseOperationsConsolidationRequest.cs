using Vixly.Operations.Domain.Bundles;

namespace Vixly.Operations.Application.UseCases.Bundles.ChooseOperationsConsolidation;

// =========================================================
// FILE: ChooseOperationsConsolidationRequest.cs
//
// PURPOSE:
// Switch how a consolidated shipment travels: repacked or kept together (Slice 6A, D-4; owner, A-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChooseOperationsConsolidationRequest
{
    public Guid ShipmentId { get; init; }

    public OperationsConsolidationWay Way { get; init; }
}
