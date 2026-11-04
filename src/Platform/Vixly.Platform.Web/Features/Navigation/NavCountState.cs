namespace Vixly.Platform.Web.Features.Navigation;

// =========================================================
// FILE: NavCountState.cs
//
// PURPOSE:
// State of one menu entry's count.
//
// RESPONSIBILITIES:
// - Hold the count, or none when there is nothing to show
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record NavCountState(int? Count, DateTimeOffset? LoadedUtc)
{
    public static NavCountState Initial { get; } = new(null, null);
}
