namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantChangeProofFile.cs
//
// PURPOSE:
// A change request’s proof document, for download by staff (B12).
//
// RESPONSIBILITIES:
// - Carry the bytes with their type and name
//
// IMPORTANT NOTES:
// - Loaded only for the download
// =========================================================
public sealed record PlatformTenantChangeProofFile(string FileName, string ContentType, byte[] Content);
