namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformZelleLimits.cs
//
// PURPOSE:
// Sizes and limits for Zelle settings and claims.
//
// RESPONSIBILITIES:
// - Field lengths, QR image size, claim limits
//
// IMPORTANT NOTES:
// - Zelle moves US dollars only
// =========================================================
public static class PlatformZelleLimits
{
    public const string Currency = "USD";

    public const int RecipientMaxLength = 100;

    public const int EmailMaxLength = 254;

    public const int TagMaxLength = 50;

    public const int PhoneMaxLength = 32;

    public const int MaxQrKilobytes = 256;

    public const int SenderMaxLength = 100;

    public const int ReferenceMaxLength = 60;

    public const int ReasonMaxLength = 500;

    public const int NoteMaxLength = 1000;

    // A claim may name a send date at most this many days back.
    public const int MaxDaysBack = 30;

    // Claims per invoice, rejected ones included; after that the customer contacts Vixly.
    public const int MaxClaimsPerInvoice = 3;
}
