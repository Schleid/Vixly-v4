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
// - Hold the checkout page look (name, logo, icon, colours) and the bank statement wording
//
// IMPORTANT NOTES:
// - One row; holds no secret (keys come from configuration, S-11)
// - Card payments are offered only when on, checked and the currency is accepted
// - Empty look fields mean the provider dashboard's own branding is used
// - The statement prefix is set at the provider (shortened descriptor); Vixly stores it only to tell tenants how the charge reads
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

    public string? CheckoutDisplayName { get; private set; }

    public string? CheckoutLogoUrl { get; private set; }

    public string? CheckoutIconUrl { get; private set; }

    // "#RRGGBB"
    public string? CheckoutBackgroundColor { get; private set; }

    public string? CheckoutButtonColor { get; private set; }

    // The account's shortened descriptor at the provider, e.g. "TECHCRAFT" (2 to 10 characters, as set at the provider).
    public string? StatementPrefix { get; private set; }

    // Added to every charge, e.g. "VIXLY".
    public string? StatementSuffix { get; private set; }

    // How the charge reads on the payer's statement, e.g. "TECHCRAFT* VIXLY"; null when not known.
    public string? StatementWording => StatementPrefix is null || StatementSuffix is null ? null : StatementPrefix + "* " + StatementSuffix;

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

    public void UpdateCheckout(
        string? displayName,
        string? logoUrl,
        string? iconUrl,
        string? backgroundColor,
        string? buttonColor,
        string? statementPrefix,
        string? statementSuffix,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformPaymentsErrors.ActorRequired);
        }

        string? name = Optional(displayName);

        if (name is not null && (name.Length > PlatformPaymentsLimits.CheckoutDisplayNameMaxLength || name.Any(char.IsControl)))
        {
            throw new DomainException(PlatformPaymentsErrors.CheckoutSettingInvalid);
        }

        string? prefix = Statement(statementPrefix);
        string? suffix = Statement(statementSuffix);

        if ((prefix is not null && (prefix.Length < PlatformPaymentsLimits.StatementPrefixMinLength || prefix.Length > PlatformPaymentsLimits.StatementPrefixMaxLength))
            || (suffix is not null && suffix.Length > PlatformPaymentsLimits.StatementMaxLength - 2 - (prefix?.Length ?? PlatformPaymentsLimits.StatementPrefixMinLength)))
        {
            throw new DomainException(PlatformPaymentsErrors.StatementSuffixInvalid);
        }

        CheckoutDisplayName = name;
        CheckoutLogoUrl = ImageUrl(logoUrl);
        CheckoutIconUrl = ImageUrl(iconUrl);
        CheckoutBackgroundColor = Color(backgroundColor);
        CheckoutButtonColor = Color(buttonColor);
        StatementPrefix = prefix;
        StatementSuffix = suffix;
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

    private static string? Optional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Absolute https address of an image the provider downloads.
    private static string? ImageUrl(string? value)
    {
        string? url = Optional(value);

        if (url is null)
        {
            return null;
        }

        return url.Length <= PlatformPaymentsLimits.CheckoutImageUrlMaxLength
            && !url.Any(char.IsWhiteSpace)
            && Uri.TryCreate(url, UriKind.Absolute, out Uri? address)
            && address.Scheme == Uri.UriSchemeHttps
            ? url
            : throw new DomainException(PlatformPaymentsErrors.CheckoutSettingInvalid);
    }

    private static string? Color(string? value)
    {
        string? color = Optional(value);

        if (color is null)
        {
            return null;
        }

        return color.Length == 7 && color[0] == '#' && color.Skip(1).All(char.IsAsciiHexDigit)
            ? color.ToUpperInvariant()
            : throw new DomainException(PlatformPaymentsErrors.CheckoutSettingInvalid);
    }

    // Provider rule: Latin letters, digits, spaces, dots and dashes, with at least one letter; stored in capitals.
    private static string? Statement(string? value)
    {
        string? text = Optional(value);

        if (text is null)
        {
            return null;
        }

        string upper = text.ToUpperInvariant();

        return upper.All(character => char.IsAsciiLetterUpper(character) || char.IsAsciiDigit(character) || character is ' ' or '.' or '-')
            && upper.Any(char.IsAsciiLetterUpper)
            ? upper
            : throw new DomainException(PlatformPaymentsErrors.StatementSuffixInvalid);
    }
}
