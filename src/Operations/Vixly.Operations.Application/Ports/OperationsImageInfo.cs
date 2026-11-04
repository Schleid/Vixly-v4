namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsImageInfo.cs
//
// PURPOSE:
// What an uploaded file really is.
//
// RESPONSIBILITIES:
// - Carry the content type, dimensions and hash
//
// IMPORTANT NOTES:
// - Read from the file bytes, never from the file name
// =========================================================
public sealed record OperationsImageInfo(string ContentType, int Width, int Height, string Sha256);
