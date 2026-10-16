namespace Vixly.Platform.Application.UseCases.Collections;

// =========================================================
// FILE: PlatformCollectionsSettingsItem.cs
//
// PURPOSE:
// Collections settings as the settings page shows them (D-C12).
//
// RESPONSIBILITIES:
// - Carry every setting and whether the reader may change them
//
// IMPORTANT NOTES:
// - BlockedReadOnlyDays: 0 = no access at once; null = read-only until paid
// =========================================================
public sealed record PlatformCollectionsSettingsItem(
    bool Enabled,
    int Reminder1Day,
    int Reminder2Day,
    int FinalWarningDay,
    int BlockDay,
    IReadOnlyList<int> RetryDays,
    int MinimumNoticeDays,
    int? BlockedReadOnlyDays,
    int ExtensionMaxDays,
    int ExtensionsPerCase,
    int PendingPaymentReadOnlyDays,
    int LongBlockDays,
    DateTimeOffset? UpdatedUtc,
    bool CanEdit);
