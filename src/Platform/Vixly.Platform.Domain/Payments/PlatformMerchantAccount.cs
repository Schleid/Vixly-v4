using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformMerchantAccount.cs
//
// PURPOSE:
// Vixly's own merchant account with the card provider (D-P1, D-P8, D-P9, D-P11).
//
// RESPONSIBILITIES:
// - Hold provider, mode, publishable key, accepted currencies and whether card payments are on
// - Record the result of the last connection check
//
// IMPORTANT NOTES:
// - One row; holds no secret (keys come from configuration, S-11)
// - Card payments are offered only when on, checked and the currency is accepted
// =========================================================
public sealed class PlatformMerchantAccount : AuditableEntity
{
    private PlatformMerchantAccount()
    {
        Key = PlatformPaymentsLimits.SingletonKey;
        AcceptedCurrencies = string.Empty;
    }

    private PlatformMerchantAccount(PlatformCardProvider provider, DateTimeOffset utcNow)
        : base(utcNow)
    {
        Key = PlatformPaymentsLimits.SingletonKey;
        Provider = provider;
        Mode = PlatformPaymentMode.Test;
        AcceptedCurrencies = string.Empty;
    }

    public string Key { get; private set; }

    public PlatformCardProvider Provider { get; private set; }

    public PlatformPaymentMode Mode { get; private set; }

    public string? PublishableKey { get; private set; }

    // Stored as "USD,EUR"; read through Currencies.
    public string AcceptedCurrencies { get; private set; }

    public bool CardPaymentsEnabled { get; private set; }

    public DateTimeOffset? LastCheckedUtc { get; private set; }

    public bool LastCheckSucceeded { get; private set; }

    public string? LastCheckCode { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public IReadOnlyList<string> Currencies => AcceptedCurrencies.Length == 0
        ? Array.Empty<string>()
        : AcceptedCurrencies.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public static PlatformMerchantAccount Create(PlatformCardProvider provider, DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(provider))
        {
            throw new DomainException(PlatformPaymentsErrors.NotReady);
        }

        return new PlatformMerchantAccount(provider, utcNow);
    }

    // The mode comes from the configured secret key; the publishable key must match it.
    public void Update(PlatformCardProvider provider, PlatformPaymentMode mode, string? publishableKey, IEnumerable<string> currencies, bool enabled, Guid actorUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(currencies);

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformPaymentsErrors.ActorRequired);
        }

        if (!Enum.IsDefined(provider) || !Enum.IsDefined(mode))
        {
            throw new DomainException(PlatformPaymentsErrors.NotReady);
        }

        string? key = string.IsNullOrWhiteSpace(publishableKey) ? null : publishableKey.Trim();

        if (key is not null && (key.Length > PlatformPaymentsLimits.PublishableKeyMaxLength || key.Any(char.IsWhiteSpace)
            || (provider == PlatformCardProvider.Stripe && !key.StartsWith(mode == PlatformPaymentMode.Live ? "pk_live_" : "pk_test_", StringComparison.Ordinal))))
        {
            throw new DomainException(PlatformPaymentsErrors.PublishableKeyInvalid);
        }

        string[] codes = currencies.Select(PlatformPaymentsLimits.Currency).Distinct(StringComparer.Ordinal).OrderBy(code => code, StringComparer.Ordinal).ToArray();

        if (codes.Length > PlatformPaymentsLimits.MaxCurrencies)
        {
            throw new DomainException(PlatformPaymentsErrors.TooManyCurrencies);
        }

        if (enabled && (codes.Length == 0 || (provider == PlatformCardProvider.Stripe && key is null)))
        {
            throw new DomainException(PlatformPaymentsErrors.NotReady);
        }

        if (provider != Provider || mode != Mode)
        {
            // Another provider or mode is another account: the last check no longer applies (V3 defect 10).
            LastCheckedUtc = null;
            LastCheckSucceeded = false;
            LastCheckCode = null;
        }

        Provider = provider;
        Mode = mode;
        PublishableKey = key;
        AcceptedCurrencies = string.Join(',', codes);
        CardPaymentsEnabled = enabled;
        UpdatedByUserId = actorUserId;
        MarkAsModified(utcNow);
    }

    public void RecordCheck(bool succeeded, string? code, DateTimeOffset utcNow)
    {
        LastCheckedUtc = utcNow;
        LastCheckSucceeded = succeeded;
        LastCheckCode = succeeded ? null : PlatformPaymentsLimits.Code(code, PlatformPaymentsLimits.FailureCodeMaxLength);
        MarkAsModified(utcNow);
    }

    public bool IsReady => CardPaymentsEnabled && LastCheckSucceeded;

    public bool Accepts(string currencyCode)
    {
        return IsReady && Currencies.Contains(currencyCode, StringComparer.Ordinal);
    }
}
