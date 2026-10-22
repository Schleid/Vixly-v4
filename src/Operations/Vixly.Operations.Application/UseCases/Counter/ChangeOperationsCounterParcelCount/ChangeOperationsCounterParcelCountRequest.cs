namespace Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterParcelCount;

// =========================================================
// FILE: ChangeOperationsCounterParcelCountRequest.cs
//
// PURPOSE:
// Corrects the parcel count of a shipment made at the counter, before its first parcel (Slice 5C, C-1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsCounterParcelCountRequest
{
    public Guid ShipmentId { get; init; }

    public int ParcelCount { get; init; }
}
