namespace Vixly.Operations.Application.UseCases.Receiving.PutAwayOperationsParcel;

// =========================================================
// FILE: PutAwayOperationsParcelRequest.cs
//
// PURPOSE:
// Puts a parcel on a shelf of its warehouse (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PutAwayOperationsParcelRequest
{
    public Guid ParcelId { get; init; }

    public Guid StorageLocationId { get; init; }

    public string CommandKey { get; init; } = string.Empty;
}
