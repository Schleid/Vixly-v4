using Vixly.Platform.Domain.Collections;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformCollectionsDashboard.cs
//
// PURPOSE:
// The collections dashboard figures (D-C13).
//
// RESPONSIBILITIES:
// - Carry counts and amounts computed by aggregate queries
//
// IMPORTANT NOTES:
// - Amounts per currency; never converted
// - Ageing buckets: 1-30, 31-60, 61-90, over 90 days overdue
// =========================================================
public sealed record PlatformCollectionsDashboard(
    IReadOnlyDictionary<PlatformCollectionStage, int> OpenByStage,
    int Blocked,
    int ExtensionsEndingSoon,
    int LongBlocks,
    int FailedRetries,
    IReadOnlyList<PlatformMoneyItem> Overdue,
    IReadOnlyList<PlatformAgeingItem> Ageing,
    IReadOnlyList<PlatformMoneyItem> Recovered);
