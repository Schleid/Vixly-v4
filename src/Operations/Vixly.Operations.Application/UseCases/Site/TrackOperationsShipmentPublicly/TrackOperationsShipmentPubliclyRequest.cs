namespace Vixly.Operations.Application.UseCases.Site.TrackOperationsShipmentPublicly;

// =========================================================
// FILE: TrackOperationsShipmentPubliclyRequest.cs
//
// PURPOSE:
// Public tracking by number: milestones and dates only (Slice 1B, owner decision; ADR-0021).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class TrackOperationsShipmentPubliclyRequest
{
    public string Number { get; init; } = string.Empty;
}
