using System.Globalization;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Collections;

// =========================================================
// FILE: PlatformCollectionsSettings.cs
//
// PURPOSE:
// The one row of collections settings: the timeline, the block levels, extensions and Pending payment (D-C12).
//
// RESPONSIBILITIES:
// - Start with the owner's defaults (14-day timeline, read-only for 7 days, then no access)
// - Check every change against the bounds and the order of the steps
// - Give the access rules the subscription needs
//
// IMPORTANT NOTES:
// - Changes apply to future steps only; a step already taken is never undone
// - BlockedReadOnlyDays: 0 = no access at once; null = read-only until paid (owner, 2026-10-16)
// =========================================================
public sealed class PlatformCollectionsSettings : AuditableEntity
{
    private PlatformCollectionsSettings()
    {
        Key = PlatformCollectionsLimits.SingletonKey;
        RetryDays = string.Empty;
    }

    private PlatformCollectionsSettings(DateTimeOffset utcNow)
        : base(utcNow)
    {
        Key = PlatformCollectionsLimits.SingletonKey;
        Enabled = true;
        Reminder1Day = 1;
        Reminder2Day = 5;
        FinalWarningDay = 10;
        BlockDay = 14;
        RetryDays = "3,7";
        MinimumNoticeDays = 3;
        BlockedReadOnlyDays = 7;
        ExtensionMaxDays = 14;
        ExtensionsPerCase = 2;
        PendingPaymentReadOnlyDays = 7;
        LongBlockDays = 60;
    }

    public string Key { get; private set; }

    public bool Enabled { get; private set; }

    public int Reminder1Day { get; private set; }

    public int Reminder2Day { get; private set; }

    public int FinalWarningDay { get; private set; }

    public int BlockDay { get; private set; }

    // Stored as "3,7"; read through RetryDayList.
    public string RetryDays { get; private set; }

    public int MinimumNoticeDays { get; private set; }

    public int? BlockedReadOnlyDays { get; private set; }

    public int ExtensionMaxDays { get; private set; }

    public int ExtensionsPerCase { get; private set; }

    public int PendingPaymentReadOnlyDays { get; private set; }

    public int LongBlockDays { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public IReadOnlyList<int> RetryDayList => Parse(RetryDays);

    public PlatformSubscriptionAccessRules AccessRules => new(
        TimeSpan.FromDays(PendingPaymentReadOnlyDays),
        BlockedReadOnlyDays is int days ? TimeSpan.FromDays(days) : null);

    public static PlatformCollectionsSettings Create(DateTimeOffset utcNow)
    {
        return new PlatformCollectionsSettings(utcNow);
    }

    public void Update(
        bool enabled,
        int reminder1Day,
        int reminder2Day,
        int finalWarningDay,
        int blockDay,
        IReadOnlyCollection<int> retryDays,
        int minimumNoticeDays,
        int? blockedReadOnlyDays,
        int extensionMaxDays,
        int extensionsPerCase,
        int pendingPaymentReadOnlyDays,
        int longBlockDays,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(retryDays);

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformCollectionsErrors.ActorRequired);
        }

        int[] retries = retryDays.Distinct().Order().ToArray();
        bool valid = reminder1Day >= 1
            && reminder1Day < reminder2Day
            && reminder2Day < finalWarningDay
            && finalWarningDay < blockDay
            && blockDay <= PlatformCollectionsLimits.MaxBlockDay
            && minimumNoticeDays is >= 1 and <= PlatformCollectionsLimits.MaxNoticeDays
            && blockDay - finalWarningDay >= minimumNoticeDays
            && retries.Length <= PlatformCollectionsLimits.MaxRetryDays
            && retries.All(day => day >= 1 && day < blockDay)
            && blockedReadOnlyDays is null or (>= 0 and <= PlatformCollectionsLimits.MaxBlockedReadOnlyDays)
            && extensionMaxDays is >= 1 and <= PlatformCollectionsLimits.MaxExtensionDays
            && extensionsPerCase is >= 0 and <= PlatformCollectionsLimits.MaxExtensionsPerCase
            && pendingPaymentReadOnlyDays is >= 0 and <= PlatformCollectionsLimits.MaxPendingPaymentReadOnlyDays
            && longBlockDays is >= PlatformCollectionsLimits.MinLongBlockDays and <= PlatformCollectionsLimits.MaxLongBlockDays;

        if (!valid)
        {
            throw new DomainException(PlatformCollectionsErrors.SettingsInvalid);
        }

        Enabled = enabled;
        Reminder1Day = reminder1Day;
        Reminder2Day = reminder2Day;
        FinalWarningDay = finalWarningDay;
        BlockDay = blockDay;
        RetryDays = string.Join(',', retries.Select(day => day.ToString(CultureInfo.InvariantCulture)));
        MinimumNoticeDays = minimumNoticeDays;
        BlockedReadOnlyDays = blockedReadOnlyDays;
        ExtensionMaxDays = extensionMaxDays;
        ExtensionsPerCase = extensionsPerCase;
        PendingPaymentReadOnlyDays = pendingPaymentReadOnlyDays;
        LongBlockDays = longBlockDays;
        UpdatedByUserId = actorUserId;
        MarkAsModified(utcNow);
    }

    private static int[] Parse(string value)
    {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => int.TryParse(item, NumberStyles.None, CultureInfo.InvariantCulture, out int day) ? day : 0)
            .Where(day => day > 0)
            .ToArray();
    }
}
