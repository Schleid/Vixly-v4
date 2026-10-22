namespace Vixly.Operations.Application.UseCases.Bundles.TakeOutOfOperationsRepackBox;

// =========================================================
// FILE: TakeOutOfOperationsRepackBoxRequest.cs
//
// PURPOSE:
// Take a parcel out of an open repack box (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class TakeOutOfOperationsRepackBoxRequest
{
    public Guid BoxId { get; init; }

    public Guid ParcelId { get; init; }
}
