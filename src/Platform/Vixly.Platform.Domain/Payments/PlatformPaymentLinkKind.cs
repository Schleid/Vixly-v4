namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformPaymentLinkKind.cs
//
// PURPOSE:
// What a payment link opens.
//
// RESPONSIBILITIES:
// - Name the what a payment link opens
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformPaymentLinkKind
{
    PayInvoice = 1,
    SaveCard = 2
}
