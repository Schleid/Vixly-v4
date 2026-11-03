using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformTenantShipmentUsage.cs
//
// PURPOSE:
// How many shipments a company created in one calendar month, as Operations reports it (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Keep the latest count of the month and whether the month is closed
// - Record once that the shipments above the cap were charged
//
// IMPORTANT NOTES:
// - One row per company and month ("2026-10"); the month follows the company's time zone (Operations decides it)
// - A count never goes down after the month is charged; nothing is charged twice
// =========================================================
public sealed class PlatformTenantShipmentUsage : AuditableEntity
{
    public const int MonthLength = 7;

    private PlatformTenantShipmentUsage()
    {
        Month = string.Empty;
    }

    private PlatformTenantShipmentUsage(Guid platformTenantId, string month, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = platformTenantId;
        Month = month;
    }

    public Guid PlatformTenantId { get; private set; }

    public string Month { get; private set; }

    public int Shipments { get; private set; }

    public bool IsFinal { get; private set; }

    public DateTimeOffset ReportedUtc { get; private set; }

    // The cap and the shipments above it when the month was charged.
    public int? Cap { get; private set; }

    public int? ExtraShipments { get; private set; }

    public decimal? ChargedAmount { get; private set; }

    public string? CurrencyCode { get; private set; }

    public DateTimeOffset? ChargedUtc { get; private set; }

    public bool IsCharged => ChargedUtc is not null;

    public static PlatformTenantShipmentUsage Start(Guid platformTenantId, string? month, DateTimeOffset utcNow)
    {
        if (platformTenantId == Guid.Empty)
        {
            throw new DomainException(PlatformSubscriptionErrors.TenantRequired);
        }

        return new PlatformTenantShipmentUsage(platformTenantId, MonthOf(month), utcNow);
    }

    public static string MonthOf(string? month)
    {
        string text = month?.Trim() ?? string.Empty;

        return text.Length == MonthLength
            && DateTime.TryParseExact(text + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime first)
            && first.Year is >= 2020 and <= 2200
                ? text
                : throw new DomainException(PlatformSubscriptionErrors.UsageMonthInvalid);
    }

    public void Report(int shipments, bool isFinal, DateTimeOffset utcNow)
    {
        if (shipments < 0 || shipments > PlatformSubscriptionLimits.MaxLimitValue * 10)
        {
            throw new DomainException(PlatformSubscriptionErrors.UsageCountInvalid);
        }

        if (IsCharged)
        {
            return;
        }

        Shipments = shipments;
        IsFinal = IsFinal || isFinal;
        ReportedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    public void MarkCharged(int cap, int extraShipments, decimal amount, string currencyCode, DateTimeOffset utcNow)
    {
        if (IsCharged)
        {
            throw new DomainException(PlatformSubscriptionErrors.UsageAlreadyCharged);
        }

        Cap = cap;
        ExtraShipments = extraShipments;
        ChargedAmount = amount;
        CurrencyCode = currencyCode;
        ChargedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // A closed month with nothing above the cap (or no cap) is settled without a charge.
    public void MarkSettled(int cap, DateTimeOffset utcNow)
    {
        if (IsCharged)
        {
            return;
        }

        Cap = cap;
        ExtraShipments = 0;
        ChargedAmount = 0m;
        ChargedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
