namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformZelleImage.cs
//
// PURPOSE:
// The Zelle QR code image.
//
// RESPONSIBILITIES:
// - Carry the bytes, their type and the version for cache busting
//
// IMPORTANT NOTES:
// - PNG or JPEG only (checked when saved)
// =========================================================
public sealed record PlatformZelleImage(byte[] Content, string ContentType, int Version, bool Enabled);
