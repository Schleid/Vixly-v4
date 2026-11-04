using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Platform;

// =========================================================
// FILE: OperationsPlatformFeedPosition.cs
//
// PURPOSE:
// How far Operations has read the Platform event feed (Stage 3, Slice 0, D-F3).
//
// RESPONSIBILITIES:
// - Keep the last sequence number read, so a restart continues where it stopped
//
// IMPORTANT NOTES:
// - A system entity: one row per feed
// - The position only moves forward
// =========================================================
public sealed class OperationsPlatformFeedPosition : AuditableEntity, IOperationsSystemEntity
{
    public const string PlatformEvents = "platform-events";

    private OperationsPlatformFeedPosition()
    {
        Feed = string.Empty;
    }

    private OperationsPlatformFeedPosition(string feed, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        Feed = feed;
    }

    public string Feed { get; private set; }

    public long Position { get; private set; }

    // =====================================================
    // METHOD: CREATE
    // =====================================================
    public static OperationsPlatformFeedPosition Create(string feed, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feed);
        return new OperationsPlatformFeedPosition(feed, utcNow);
    }

    // =====================================================
    // METHOD: MOVE TO
    // =====================================================
    public void MoveTo(long position, DateTimeOffset utcNow)
    {
        if (position <= Position)
        {
            return;
        }

        Position = position;
        MarkAsModified(utcNow);
    }
}
