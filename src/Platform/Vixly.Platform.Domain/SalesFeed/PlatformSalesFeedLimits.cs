namespace Vixly.Platform.Domain.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedLimits.cs
//
// PURPOSE:
// The fixed limits of the sales feed contract with TechOS (contract version 1).
//
// RESPONSIBILITIES:
// - Hold field lengths, the batch size, the retry window and the backoff steps
//
// IMPORTANT NOTES:
// - Contract values agreed with TechOS; changing one is a contract change
// =========================================================
public static class PlatformSalesFeedLimits
{
    public const string SingletonKey = "default";

    public const int ContractVersion = 1;

    // TechOS refuses a batch of more than 100 events.
    public const int MaxBatch = 100;

    public const int TypeMaxLength = 40;

    public const int ErrorMaxLength = 200;

    public const int ProblemMaxLength = 40;

    // Not acknowledged for this long: staff are alerted (sending goes on).
    public static TimeSpan RetryWindow { get; } = TimeSpan.FromHours(24);

    // Waits after each failed attempt, the last one repeating.
    public static IReadOnlyList<TimeSpan> Backoff { get; } = new[]
    {
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1)
    };

    public static string Text(string? value, int maxLength)
    {
        string text = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
