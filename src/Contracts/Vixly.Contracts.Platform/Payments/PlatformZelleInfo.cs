namespace Vixly.Contracts.Platform.Payments;

// =========================================================
// FILE: PlatformZelleInfo.cs
//
// PURPOSE:
// Zelle at checkout: where to send the money, the QR image and whether a payment can be reported.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - QrCodeUrl is relative to the Platform.Api base address, or null without a QR image
// - US dollars only
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformZelleInfo(
    string RecipientName,
    string? Email,
    string? Tag,
    string? Phone,
    string? QrCodeUrl,
    PlatformZelleReportInfo? Waiting,
    bool CanReport);
