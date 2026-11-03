namespace Vixly.Operations.Application.UseCases.Site.EstimateOperationsPublicShipment;

// =========================================================
// FILE: EstimateOperationsPublicShipmentRequest.cs
//
// PURPOSE:
// A visitor's estimate on the company's website (owner, 2026-10-30): a weight to a pickup destination with a published service.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class EstimateOperationsPublicShipmentRequest
{
    public string? Service { get; init; }

    public Guid? DestinationId { get; init; }

    public decimal? Weight { get; init; }
}
