using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSubscriptionMailer.cs
//
// PURPOSE:
// Sends subscription notices to tenant contacts.
//
// RESPONSIBILITIES:
// - Tell a contact that the price of their next period goes up (D-S13)
//
// IMPORTANT NOTES:
// - Written in the tenant language; amounts before tax
// =========================================================
public interface IPlatformSubscriptionMailer
{
    Task SendPriceIncreaseAsync(
        PlatformTenant tenant,
        PlatformTenantContact recipient,
        string planName,
        PlatformSubscriptionTerm term,
        decimal currentAmount,
        decimal newAmount,
        string currencyCode,
        DateTimeOffset effectiveUtc,
        CancellationToken cancellationToken);
}
