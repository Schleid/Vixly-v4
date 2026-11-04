namespace Vixly.Operations.Application.UseCases.Receiving.ScanOperationsParcel;

// =========================================================
// FILE: ScanOperationsParcelRequest.cs
//
// PURPOSE:
// Receives one package at the dock by its carrier barcode and prints its ID label (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ScanOperationsParcelRequest
{
    public Guid DeliveryId { get; init; }

    public string Barcode { get; init; } = string.Empty;

    public string ScanKey { get; init; } = string.Empty;

    public bool Damaged { get; init; }

    public Guid? PrinterId { get; init; }

    public string PortalSlug { get; init; } = string.Empty;
}
