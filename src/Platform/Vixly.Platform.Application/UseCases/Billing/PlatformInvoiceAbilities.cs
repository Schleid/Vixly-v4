namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformInvoiceAbilities.cs
//
// PURPOSE:
// What the viewer may do on an invoice.
//
// RESPONSIBILITIES:
// - Carry what the console shows
//
// IMPORTANT NOTES:
// - Plain record for the console
// =========================================================
public sealed record PlatformInvoiceAbilities(bool CanRecordPayments, bool CanRequestCredits, bool CanApproveCredits)
{
    public static PlatformInvoiceAbilities None { get; } = new(false, false, false);
}
