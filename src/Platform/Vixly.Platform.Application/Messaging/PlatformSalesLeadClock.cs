using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformSalesLeadClock.cs
//
// PURPOSE:
// Deletes closed contact requests after 24 months (Stage 2, privacy).
//
// RESPONSIBILITIES:
// - Remove old closed leads
//
// IMPORTANT NOTES:
// - Run by the Platform worker with the other clocks
// =========================================================
public sealed class PlatformSalesLeadClock
{
    private readonly IPlatformSalesLeadRepository _leads;

    private readonly TimeProvider _timeProvider;

    public PlatformSalesLeadClock(IPlatformSalesLeadRepository leads, TimeProvider timeProvider)
    {
        _leads = leads ?? throw new ArgumentNullException(nameof(leads));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task<int> RunAsync(CancellationToken cancellationToken)
    {
        return _leads.DeleteClosedBeforeAsync(_timeProvider.GetUtcNow() - PlatformSalesLeadLimits.ClosedRetention, cancellationToken);
    }
}
