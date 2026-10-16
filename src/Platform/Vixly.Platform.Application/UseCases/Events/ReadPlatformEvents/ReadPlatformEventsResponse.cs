using Vixly.Platform.Domain.Messaging;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Events.ReadPlatformEvents;

// =========================================================
// FILE: ReadPlatformEventsResponse.cs
//
// PURPOSE:
// Public events after a cursor, oldest first, for Operations and Edge (Slice 9, D-K3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReadPlatformEventsResponse
{
    private ReadPlatformEventsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformPublishedEvent> Items { get; private init; } = Array.Empty<PlatformPublishedEvent>();

    public long Next { get; private init; }

    public bool More { get; private init; }

    public static ReadPlatformEventsResponse Success(IReadOnlyList<PlatformPublishedEvent> items, long next, bool more)
    {
        return new ReadPlatformEventsResponse { Succeeded = true, Items = items, Next = next, More = more };
    }

    public static ReadPlatformEventsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReadPlatformEventsResponse { Error = error };
    }
}
