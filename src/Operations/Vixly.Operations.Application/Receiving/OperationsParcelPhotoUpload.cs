namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsParcelPhotoUpload.cs
//
// PURPOSE:
// One photo taken at receiving (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the kind and the bytes
//
// IMPORTANT NOTES:
// - Checked by the image inspector before it is stored
// =========================================================
public sealed record OperationsParcelPhotoUpload(string Kind, byte[] Content);
