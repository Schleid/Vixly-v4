using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Hosting;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Mail;

// =========================================================
// FILE: SmtpPlatformTenantMailer.cs
//
// PURPOSE:
// Sends tenant management emails: registration links and status
// notices (D-T18).
//
// RESPONSIBILITIES:
// - Registration link in the registrant's language, to the public
//   site
// - Status notice in the tenant's language, with the category
//   but never the internal reason
//
// IMPORTANT NOTES:
// - These are Vixly emails; Control is Vixly's (ADR-0012)
// - Delivery goes through PlatformMailSender
// =========================================================
public sealed class SmtpPlatformTenantMailer : IPlatformTenantMailer
{
    private readonly PlatformMailSender _sender;

    private readonly PlatformHostingOptions _hosting;

    private readonly IPlatformTenantPolicy _policy;

    public SmtpPlatformTenantMailer(PlatformMailSender sender, IOptions<PlatformHostingOptions> hosting, IPlatformTenantPolicy policy)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _hosting = hosting?.Value ?? throw new ArgumentNullException(nameof(hosting));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public Task SendRegistrationLinkAsync(PlatformTenantRegistration registration, string token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        Dictionary<string, object> values = new(StringComparer.Ordinal)
        {
            ["name"] = registration.OwnerName,
            ["company"] = registration.Company.DisplayName,
            ["address"] = $"{registration.Slug}.{_policy.TenantBaseDomain}",
            ["hours"] = (int)PlatformTenantLimits.VerificationLinkLifetime.TotalHours,
            ["link"] = $"{_hosting.PublicBaseUrl.TrimEnd('/')}/register/verify?token={Uri.EscapeDataString(token)}"
        };

        return _sender.SendAsync(
            registration.OwnerEmail.Value,
            registration.OwnerName,
            registration.Language,
            text => (VixlyText.Format(text, "email.registration.subject"), VixlyText.Format(text, "email.registration.body", values)),
            cancellationToken);
    }

    public Task SendStatusNoticeAsync(
        PlatformTenant tenant,
        PlatformTenantContact recipient,
        PlatformTenantStatus status,
        PlatformSuspensionCategory? category,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(recipient);

        string bodyKey = status switch
        {
            PlatformTenantStatus.Suspended => "email.tenant_notice.suspended",
            PlatformTenantStatus.Deactivated => "email.tenant_notice.deactivated",
            PlatformTenantStatus.Banned => "email.tenant_notice.banned",
            _ => "email.tenant_notice.reactivated"
        };

        return _sender.SendAsync(
            recipient.Email.Value,
            recipient.Name,
            tenant.DefaultLanguage,
            text => (
                VixlyText.Format(text, "email.tenant_notice.subject", "company", tenant.Company.DisplayName),
                VixlyText.Format(
                    text,
                    bodyKey,
                    new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["name"] = recipient.Name,
                        ["company"] = tenant.Company.DisplayName,
                        ["category"] = category is null ? string.Empty : text["ui.tenants.category." + CategoryKey(category.Value)].Value,
                        ["support"] = _sender.SupportAddress
                    })),
            cancellationToken);
    }

    private static string CategoryKey(PlatformSuspensionCategory category)
    {
        return category switch
        {
            PlatformSuspensionCategory.Compliance => "compliance",
            PlatformSuspensionCategory.Security => "security",
            PlatformSuspensionCategory.FraudInvestigation => "fraud_investigation",
            _ => "administrative"
        };
    }
}
