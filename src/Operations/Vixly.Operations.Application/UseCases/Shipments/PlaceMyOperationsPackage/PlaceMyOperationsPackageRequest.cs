namespace Vixly.Operations.Application.UseCases.Shipments.PlaceMyOperationsPackage;

// =========================================================
// FILE: PlaceMyOperationsPackageRequest.cs
//
// PURPOSE:
// A customer puts one of its packages into one of its shipments, moves it, or takes it out (Slice 4D; owner, 2026-10-19).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PlaceMyOperationsPackageRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid? PreAlertId { get; init; }

    public Guid? ParcelId { get; init; }

    public Guid? ShipmentId { get; init; }
}
