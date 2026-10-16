namespace Vixly.Platform.Application.UseCases.Payments.ChargePlatformInvoiceCard;

// =========================================================
// FILE: ChargePlatformInvoiceCardRequest.cs
//
// PURPOSE:
// Charges the tenant's default saved card for an invoice's balance (D-P4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChargePlatformInvoiceCardRequest
{
    public Guid PlatformInvoiceId { get; init; }
}
