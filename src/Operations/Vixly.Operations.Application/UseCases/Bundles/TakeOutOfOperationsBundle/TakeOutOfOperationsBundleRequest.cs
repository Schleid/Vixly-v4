namespace Vixly.Operations.Application.UseCases.Bundles.TakeOutOfOperationsBundle;

// =========================================================
// FILE: TakeOutOfOperationsBundleRequest.cs
//
// PURPOSE:
// Take a parcel or a repack box out of an open bundle: a wrong scan (Slice 6A, D-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class TakeOutOfOperationsBundleRequest
{
    public Guid BundleId { get; init; }

    public Guid? ParcelId { get; init; }

    public Guid? BoxId { get; init; }
}
