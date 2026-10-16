namespace Vixly.Platform.Application.UseCases.Events.ReadPlatformEvents;

// =========================================================
// FILE: ReadPlatformEventsRequest.cs
//
// PURPOSE:
// Public events after a cursor, oldest first, for Operations and Edge (Slice 9, D-K3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReadPlatformEventsRequest
{
    public long After { get; init; }

    public int Limit { get; init; } = 200;

    public IReadOnlyCollection<string> Types { get; init; } = Array.Empty<string>();
}
