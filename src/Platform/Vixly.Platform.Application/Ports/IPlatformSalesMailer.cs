using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSalesMailer.cs
//
// PURPOSE:
// Sends sales notices: approval requests to approvers and discount-ending notices to tenants.
//
// RESPONSIBILITIES:
// - Tell an approver that something waits for them (D-S24, D-S26)
// - Tell a tenant's billing contact that a discount ends (D-S25)
//
// IMPORTANT NOTES:
// - Approvers read the console language; tenants their own language
// - Amounts before tax
// =========================================================
public interface IPlatformSalesMailer
{
    Task SendApprovalRequestAsync(
        PlatformPermissionHolderItem recipient,
        string kindKey,
        string subject,
        CancellationToken cancellationToken);

    Task SendDiscountEndingAsync(
        PlatformTenant tenant,
        PlatformTenantContact recipient,
        string planName,
        DateTimeOffset endsUtc,
        decimal nextPeriodAmount,
        string currencyCode,
        CancellationToken cancellationToken);
}
