namespace Vixly.Platform.Application.UseCases.Billing.BillPlatformChargesNow;

// =========================================================
// FILE: BillPlatformChargesNowRequest.cs
//
// PURPOSE:
// Invoices a subscription's pending one-time charges now instead of with the next invoice (D-B5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class BillPlatformChargesNowRequest
{
    public Guid PlatformTenantId { get; init; }
}
