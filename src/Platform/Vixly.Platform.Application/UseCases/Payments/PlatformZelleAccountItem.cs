namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformZelleAccountItem.cs
//
// PURPOSE:
// Vixly's Zelle details as the console and checkout read them.
//
// RESPONSIBILITIES:
// - Carry the details without the QR image bytes
//
// IMPORTANT NOTES:
// - The QR image is read on its own (GetPlatformZelleQrCode)
// =========================================================
public sealed record PlatformZelleAccountItem(
    bool Enabled,
    string RecipientName,
    string? Email,
    string? Tag,
    string? Phone,
    bool HasQrCode,
    int QrCodeVersion,
    DateTimeOffset? UpdatedUtc);
