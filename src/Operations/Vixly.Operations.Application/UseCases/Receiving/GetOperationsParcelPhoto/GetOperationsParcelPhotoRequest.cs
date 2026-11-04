namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcelPhoto;

// =========================================================
// FILE: GetOperationsParcelPhotoRequest.cs
//
// PURPOSE:
// The file of one parcel photo, for staff (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsParcelPhotoRequest
{
    public Guid ParcelId { get; init; }

    public Guid PhotoId { get; init; }
}
