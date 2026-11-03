using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedEvent.cs
//
// PURPOSE:
// One sale event for TechOS, frozen as the exact JSON it is sent as (contract version 1).
//
// RESPONSIBILITIES:
// - Hold the event id, its sequence, type, time, money and the JSON body
// - Track the answer: pending, accepted, duplicate or parked with TechOS's error
//
// IMPORTANT NOTES:
// - The Id (a UUID) is the contract's event id: unique forever, so a resend is a duplicate at TechOS
// - The JSON never changes after the event is written; only its delivery status does
// - One row per source record (unique), so the same sale is never written twice
// - Amount and currency are kept beside the JSON for the daily totals
// =========================================================
public sealed class PlatformSalesFeedEvent : AuditableEntity
{
    private PlatformSalesFeedEvent()
    {
        EventType = string.Empty;
        Payload = string.Empty;
    }

    private PlatformSalesFeedEvent(
        long sequence,
        string eventType,
        PlatformSalesFeedSource source,
        Guid? sourceId,
        DateOnly? totalsDate,
        Guid? platformTenantId,
        DateTimeOffset occurredUtc,
        decimal? amount,
        string? currencyCode,
        DateTimeOffset utcNow)
        : base(utcNow)
    {
        Sequence = sequence;
        EventType = eventType;
        Source = source;
        SourceId = sourceId;
        TotalsDate = totalsDate;
        PlatformTenantId = platformTenantId;
        OccurredUtc = occurredUtc;
        Amount = amount;
        CurrencyCode = currencyCode;
        Payload = string.Empty;
        Status = PlatformSalesFeedStatus.Pending;
    }

    public long Sequence { get; private set; }

    public string EventType { get; private set; }

    public PlatformSalesFeedSource Source { get; private set; }

    public Guid? SourceId { get; private set; }

    public DateOnly? TotalsDate { get; private set; }

    public Guid? PlatformTenantId { get; private set; }

    public DateTimeOffset OccurredUtc { get; private set; }

    public decimal? Amount { get; private set; }

    public string? CurrencyCode { get; private set; }

    public string Payload { get; private set; }

    public PlatformSalesFeedStatus Status { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? SentUtc { get; private set; }

    // The JSON is built from the new event's id; it is the contract's event id (UUID), unique forever.
    public static PlatformSalesFeedEvent Create(
        long sequence,
        string eventType,
        PlatformSalesFeedSource source,
        Guid? sourceId,
        DateOnly? totalsDate,
        Guid? platformTenantId,
        DateTimeOffset occurredUtc,
        decimal? amount,
        string? currencyCode,
        Func<Guid, string> payload,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (sequence <= 0 || string.IsNullOrWhiteSpace(eventType) || eventType.Length > PlatformSalesFeedLimits.TypeMaxLength
            || !Enum.IsDefined(source) || occurredUtc.Offset != TimeSpan.Zero
            || (source == PlatformSalesFeedSource.DailyTotals ? totalsDate is null || sourceId is not null : sourceId is null || totalsDate is not null)
            || (amount is not null && (amount < 0m || currencyCode is null || currencyCode.Length != 3)))
        {
            throw new DomainException(PlatformSalesFeedErrors.EventInvalid);
        }

        PlatformSalesFeedEvent feedEvent = new(sequence, eventType, source, sourceId, totalsDate, platformTenantId, occurredUtc, amount, currencyCode, utcNow);
        feedEvent.Payload = payload(feedEvent.Id);

        if (string.IsNullOrWhiteSpace(feedEvent.Payload))
        {
            throw new DomainException(PlatformSalesFeedErrors.EventInvalid);
        }

        return feedEvent;
    }

    public void Accept(bool duplicate, DateTimeOffset utcNow)
    {
        Status = duplicate ? PlatformSalesFeedStatus.Duplicate : PlatformSalesFeedStatus.Accepted;
        LastError = null;
        SentUtc = utcNow;
        MarkAsModified(utcNow);
    }

    public void Park(string? error, DateTimeOffset utcNow)
    {
        Status = PlatformSalesFeedStatus.Parked;
        LastError = PlatformSalesFeedLimits.Text(error, PlatformSalesFeedLimits.ErrorMaxLength);
        SentUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // Staff send a parked event again, unchanged (same id, same JSON).
    // A rejected event replaced by a corrected one under a new id; it stays on record and is never sent again.
    public void Replace(DateTimeOffset utcNow)
    {
        if (Status != PlatformSalesFeedStatus.Parked)
        {
            throw new DomainException(PlatformSalesFeedErrors.NotParked);
        }

        Status = PlatformSalesFeedStatus.Replaced;
        MarkAsModified(utcNow);
    }

    public void SendAgain(DateTimeOffset utcNow)
    {
        if (Status != PlatformSalesFeedStatus.Parked)
        {
            throw new DomainException(PlatformSalesFeedErrors.NotParked);
        }

        Status = PlatformSalesFeedStatus.Pending;
        MarkAsModified(utcNow);
    }
}
