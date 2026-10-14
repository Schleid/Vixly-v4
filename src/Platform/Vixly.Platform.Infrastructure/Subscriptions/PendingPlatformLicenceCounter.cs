using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Subscriptions;

// =========================================================
// FILE: PendingPlatformLicenceCounter.cs
//
// PURPOSE:
// Answers zero licences in use until Licensing exists.
//
// RESPONSIBILITIES:
// - Stand in for the licence count
//
// IMPORTANT NOTES:
// - Replaced in Slice 7 (Licensing)
// =========================================================
internal sealed class PendingPlatformLicenceCounter : IPlatformLicenceCounter
{
    public Task<int> CountActiveAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }
}
