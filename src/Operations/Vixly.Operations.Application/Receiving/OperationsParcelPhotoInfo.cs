namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsParcelPhotoInfo.cs
//
// PURPOSE:
// One photo of a parcel, without its file (Slice 3B).
//
// RESPONSIBILITIES:
// - Name the photo for a page that fetches it by id
//
// IMPORTANT NOTES:
// - The file is read through a separate, checked call; never by a storage address
// =========================================================
public sealed record OperationsParcelPhotoInfo(Guid PhotoId, string Kind, DateTimeOffset TakenUtc);
