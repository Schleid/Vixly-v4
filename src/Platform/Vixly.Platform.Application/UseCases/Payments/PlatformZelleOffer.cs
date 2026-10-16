namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformZelleOffer.cs
//
// PURPOSE:
// Zelle at checkout: where to send the money and whether a payment can be reported.
//
// RESPONSIBILITIES:
// - Carry the Zelle details, the QR version and any waiting report
//
// IMPORTANT NOTES:
// - Only for open US-dollar invoices while Zelle is on
// - QrCodeVersion is null when there is no QR image
// =========================================================
public sealed record PlatformZelleOffer(
    string RecipientName,
    string? Email,
    string? Tag,
    string? Phone,
    int? QrCodeVersion,
    PlatformZelleWaitingClaim? Waiting,
    bool CanReport);
