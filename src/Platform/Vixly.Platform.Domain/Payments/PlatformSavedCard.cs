using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformSavedCard.cs
//
// PURPOSE:
// A card a tenant saved at the provider: only its reference, brand, last four and expiry (D-P3).
//
// RESPONSIBILITIES:
// - Hold what staff and the tenant may see of a card
// - Know when it has expired; be removed once
//
// IMPORTANT NOTES:
// - Card numbers and CVV never reach Vixly (D-P2, PCI SAQ A)
// - The newest card becomes the default
// =========================================================
public sealed class PlatformSavedCard : AuditableEntity
{
    private PlatformSavedCard()
    {
        ProviderPaymentMethodId = string.Empty;
        Brand = string.Empty;
        Last4 = string.Empty;
    }

    private PlatformSavedCard(Guid tenantId, PlatformCardProvider provider, string methodId, string brand, string last4, int expiryMonth, int expiryYear, PlatformCardAddedVia addedVia, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = tenantId;
        Provider = provider;
        ProviderPaymentMethodId = methodId;
        Brand = brand;
        Last4 = last4;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        AddedVia = addedVia;
        Status = PlatformSavedCardStatus.Active;
        IsDefault = true;
    }

    public Guid PlatformTenantId { get; private set; }

    public PlatformCardProvider Provider { get; private set; }

    public string ProviderPaymentMethodId { get; private set; }

    public string Brand { get; private set; }

    public string Last4 { get; private set; }

    public int ExpiryMonth { get; private set; }

    public int ExpiryYear { get; private set; }

    public PlatformCardAddedVia AddedVia { get; private set; }

    public PlatformSavedCardStatus Status { get; private set; }

    public bool IsDefault { get; private set; }

    public Guid? RemovedByUserId { get; private set; }

    public DateTimeOffset? RemovedUtc { get; private set; }

    public bool IsActive => Status == PlatformSavedCardStatus.Active;

    public static PlatformSavedCard Create(Guid tenantId, PlatformCardProvider provider, string? methodId, string? brand, string? last4, int expiryMonth, int expiryYear, PlatformCardAddedVia addedVia, DateTimeOffset utcNow)
    {
        string brandText = brand?.Trim().ToLowerInvariant() ?? string.Empty;
        string digits = last4?.Trim() ?? string.Empty;

        if (tenantId == Guid.Empty || !Enum.IsDefined(addedVia)
            || brandText.Length is 0 or > PlatformPaymentsLimits.BrandMaxLength
            || digits.Length != 4 || !digits.All(char.IsAsciiDigit)
            || expiryMonth is < 1 or > 12 || expiryYear is < 2000 or > 2100)
        {
            throw new DomainException(PlatformPaymentsErrors.CardInvalid);
        }

        return new PlatformSavedCard(tenantId, provider, PlatformPaymentsLimits.ProviderId(methodId), brandText, digits, expiryMonth, expiryYear, addedVia, utcNow);
    }

    // A card works until the end of its expiry month.
    public bool IsExpiredAt(DateTimeOffset utcNow)
    {
        return (utcNow.Year, utcNow.Month).CompareTo((ExpiryYear, ExpiryMonth)) > 0;
    }

    public void SetDefault(bool isDefault, DateTimeOffset utcNow)
    {
        if (IsDefault != isDefault)
        {
            IsDefault = isDefault && IsActive;
            MarkAsModified(utcNow);
        }
    }

    public void Remove(Guid? actorUserId, DateTimeOffset utcNow)
    {
        if (!IsActive)
        {
            throw new DomainException(PlatformPaymentsErrors.CardRemoved);
        }

        Status = PlatformSavedCardStatus.Removed;
        IsDefault = false;
        RemovedByUserId = actorUserId;
        RemovedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
