namespace Vixly.Operations.Application.UseCases.Receiving.GetMyOperationsParcelPhoto;

// =========================================================
// FILE: GetMyOperationsParcelPhotoRequest.cs
//
// PURPOSE:
// The file of a photo of a customer's own package (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsParcelPhotoRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid ParcelId { get; init; }

    public Guid PhotoId { get; init; }
}
