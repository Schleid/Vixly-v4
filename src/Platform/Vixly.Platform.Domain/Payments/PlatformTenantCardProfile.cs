using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformTenantCardProfile.cs
//
// PURPOSE:
// A tenant's customer record at the card provider and its consent to automatic charging (D-P3, D-P4).
//
// RESPONSIBILITIES:
// - Hold the provider customer id
// - Record who agreed to automatic charging, with which text and when; let staff turn it off
//
// IMPORTANT NOTES:
// - Only the tenant gives consent, on the public page; staff can turn it off but never on
// - A customer belongs to one provider and mode; switching starts a new customer
// =========================================================
public sealed class PlatformTenantCardProfile : AuditableEntity
{
    private PlatformTenantCardProfile()
    {
        ProviderCustomerId = string.Empty;
    }

    private PlatformTenantCardProfile(Guid tenantId, PlatformCardProvider provider, PlatformPaymentMode mode, string customerId, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = tenantId;
        Provider = provider;
        Mode = mode;
        ProviderCustomerId = customerId;
    }

    public Guid PlatformTenantId { get; private set; }

    public PlatformCardProvider Provider { get; private set; }

    public PlatformPaymentMode Mode { get; private set; }

    public string ProviderCustomerId { get; private set; }

    public bool AutoCharge { get; private set; }

    public string? ConsentEmail { get; private set; }

    public string? ConsentTextVersion { get; private set; }

    public DateTimeOffset? ConsentUtc { get; private set; }

    public Guid? AutoChargeOffByUserId { get; private set; }

    public DateTimeOffset? AutoChargeOffUtc { get; private set; }

    public static PlatformTenantCardProfile Create(Guid tenantId, PlatformCardProvider provider, PlatformPaymentMode mode, string? customerId, DateTimeOffset utcNow)
    {
        return tenantId == Guid.Empty
            ? throw new DomainException(PlatformPaymentsErrors.ProviderReferenceInvalid)
            : new PlatformTenantCardProfile(tenantId, provider, mode, PlatformPaymentsLimits.ProviderId(customerId), utcNow);
    }

    public bool Matches(PlatformCardProvider provider, PlatformPaymentMode mode) => Provider == provider && Mode == mode;

    // Another provider or mode knows nothing of this customer: start again and drop consent (V3 defect 10).
    public void Reconnect(PlatformCardProvider provider, PlatformPaymentMode mode, string? customerId, DateTimeOffset utcNow)
    {
        Provider = provider;
        Mode = mode;
        ProviderCustomerId = PlatformPaymentsLimits.ProviderId(customerId);
        AutoCharge = false;
        MarkAsModified(utcNow);
    }

    public void GiveConsent(string? email, string? textVersion, DateTimeOffset utcNow)
    {
        string who = email?.Trim() ?? string.Empty;
        string version = textVersion?.Trim() ?? string.Empty;

        if (who.Length is 0 or > 254 || !who.Contains('@', StringComparison.Ordinal) || version.Length is 0 or > PlatformPaymentsLimits.ConsentVersionMaxLength)
        {
            throw new DomainException(PlatformPaymentsErrors.ConsentInvalid);
        }

        AutoCharge = true;
        ConsentEmail = who;
        ConsentTextVersion = version;
        ConsentUtc = utcNow;
        AutoChargeOffByUserId = null;
        AutoChargeOffUtc = null;
        MarkAsModified(utcNow);
    }

    public void TurnOffAutoCharge(Guid? actorUserId, DateTimeOffset utcNow)
    {
        if (!AutoCharge)
        {
            throw new DomainException(PlatformPaymentsErrors.AutoChargeOff);
        }

        AutoCharge = false;
        AutoChargeOffByUserId = actorUserId;
        AutoChargeOffUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
