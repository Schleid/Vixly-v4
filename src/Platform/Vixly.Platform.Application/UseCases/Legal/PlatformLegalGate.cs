using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.Legal;

// =========================================================
// FILE: PlatformLegalGate.cs
//
// PURPOSE:
// Works out where a tenant stands with the Terms, and caps its access when overdue (owner D-Lg6).
//
// RESPONSIBILITIES:
// - Status: accepted, due or overdue against the newest Terms version that needs acceptance
// - Overdue: full access becomes read-only until the owner accepts
//
// IMPORTANT NOTES:
// - Only Active and Suspended tenants are asked
// - Never makes access better, only caps it
// =========================================================
public sealed class PlatformLegalGate
{
    private readonly IPlatformLegalRepository _legal;

    private readonly IPlatformTenantRepository _tenants;

    private readonly TimeProvider _timeProvider;

    public PlatformLegalGate(IPlatformLegalRepository legal, IPlatformTenantRepository tenants, TimeProvider timeProvider)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public static bool Asked(PlatformTenant tenant) => tenant.Status is PlatformTenantStatus.Active or PlatformTenantStatus.Suspended;

    public async Task<PlatformTenantLegalState> StateAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _tenants.GetAsync(tenantId, cancellationToken) is { } tenant ? await StateAsync(tenant, cancellationToken) : PlatformTenantLegalState.NotRequired;
    }

    public async Task<PlatformTenantLegalState> StateAsync(PlatformTenant tenant, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (!Asked(tenant) || await _legal.LatestRequiredTermsAsync(cancellationToken) is not { PublishedUtc: { } published } required)
        {
            return PlatformTenantLegalState.NotRequired;
        }

        bool accepted = await _legal.HasAcceptedSinceAsync(tenant.Id, published, cancellationToken);
        DateTimeOffset deadline = PlatformLegalRules.Deadline(required, tenant.ActivatedUtc);
        return new PlatformTenantLegalState(PlatformLegalRules.Status(true, accepted, deadline, _timeProvider.GetUtcNow()), deadline, required.Id, required.Version);
    }

    public static PlatformSubscriptionAccess Cap(PlatformSubscriptionAccess access, PlatformTenantLegalState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Overdue && access == PlatformSubscriptionAccess.Full ? PlatformSubscriptionAccess.ReadOnly : access;
    }
}
