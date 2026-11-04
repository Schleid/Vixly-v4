using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsDispatchBundle.cs
//
// PURPOSE:
// A bundle carried by a dispatch (Slice 7, R-11): the dispatch keeps its bundles after they arrive and go on.
//
// RESPONSIBILITIES:
// - Link a dispatch to each bundle it carried
//
// IMPORTANT NOTES:
// - A bundle may travel on two dispatches: to a hub, then onward
// - Removed only while the dispatch is planned, or when it is cancelled
// - A scan and the end of receiving both change the row, so they never cross unseen (review)
// =========================================================
public sealed class OperationsDispatchBundle : OperationsEntity
{
    private OperationsDispatchBundle()
    {
    }

    private OperationsDispatchBundle(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public Guid DispatchId { get; private set; }

    public Guid BundleId { get; private set; }

    // Scanned in on this dispatch at its receiving warehouse (review: what arrived on this dispatch, wherever the bundle went next).
    public DateTimeOffset? ArrivedUtc { get; private set; }

    // Listed missing when receiving finished.
    public DateTimeOffset? MissingUtc { get; private set; }

    public void Arrive(DateTimeOffset utcNow)
    {
        ArrivedUtc ??= utcNow;
        MarkAsModified(utcNow);
    }

    public void Missing(DateTimeOffset utcNow)
    {
        MissingUtc ??= utcNow;
        MarkAsModified(utcNow);
    }

    public static OperationsDispatchBundle Create(Guid tenantId, Guid dispatchId, Guid bundleId, DateTimeOffset utcNow) =>
        new(tenantId, utcNow) { DispatchId = dispatchId, BundleId = bundleId };
}
