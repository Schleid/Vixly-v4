namespace Vixly.Operations.Application.UseCases.Bundles.OpenOperationsRepackBox;

// =========================================================
// FILE: OpenOperationsRepackBoxRequest.cs
//
// PURPOSE:
// Start repacking a consolidated shipment into one box (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class OpenOperationsRepackBoxRequest
{
    public Guid ShipmentId { get; init; }

    public Guid? RequestKey { get; init; }
}
