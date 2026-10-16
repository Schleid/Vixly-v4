using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformPaymentsMailer.cs
//
// PURPOSE:
// Emails about card payments to a tenant's billing contact (D-P14).
//
// RESPONSIBILITIES:
// - Send the save-card link
// - Tell the tenant a charge failed or needs them, with a pay link
//
// IMPORTANT NOTES:
// - In the tenant's language; never provider error text
// =========================================================
public interface IPlatformPaymentsMailer
{
    Task SendSaveCardLinkAsync(PlatformTenant tenant, PlatformTenantContact recipient, string link, DateTimeOffset expiresUtc, CancellationToken cancellationToken);

    Task SendCardProblemAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformInvoice invoice, bool actionRequired, string? payLink, CancellationToken cancellationToken);
}
