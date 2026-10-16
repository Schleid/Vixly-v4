namespace Vixly.Platform.Application.UseCases.Collections.UpdatePlatformCollectionsSettings;

// =========================================================
// FILE: UpdatePlatformCollectionsSettingsRequest.cs
//
// PURPOSE:
// Changes the collections timeline, block levels and extension limits (D-C12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdatePlatformCollectionsSettingsRequest
{
    public bool Enabled { get; init; }

    public int Reminder1Day { get; init; }

    public int Reminder2Day { get; init; }

    public int FinalWarningDay { get; init; }

    public int BlockDay { get; init; }

    public IReadOnlyList<int> RetryDays { get; init; } = Array.Empty<int>();

    public int MinimumNoticeDays { get; init; }

    public int? BlockedReadOnlyDays { get; init; }

    public int ExtensionMaxDays { get; init; }

    public int ExtensionsPerCase { get; init; }

    public int PendingPaymentReadOnlyDays { get; init; }

    public int LongBlockDays { get; init; }
}
