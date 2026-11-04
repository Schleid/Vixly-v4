using Vixly.Operations.Domain.Platform;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsFeedPositionRepository.cs
//
// PURPOSE:
// Stores how far Operations has read a feed.
//
// RESPONSIBILITIES:
// - Find or add the position of a feed
//
// IMPORTANT NOTES:
// - One row per feed
// =========================================================
public interface IOperationsFeedPositionRepository
{
    Task<OperationsPlatformFeedPosition?> FindAsync(string feed, CancellationToken cancellationToken);

    void Add(OperationsPlatformFeedPosition position);
}
