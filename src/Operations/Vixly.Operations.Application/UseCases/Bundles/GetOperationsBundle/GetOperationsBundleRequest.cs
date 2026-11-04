namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundle;

// =========================================================
// FILE: GetOperationsBundleRequest.cs
//
// PURPOSE:
// One bundle (Slice 6A, D-10): its parcels, its shipments' completeness, and the shipment of the last scan.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsBundleRequest
{
    public Guid BundleId { get; init; }

    // The parcel just scanned in: its shipment is shown on top (D-3).
    public Guid? FocusParcelId { get; init; }
}
