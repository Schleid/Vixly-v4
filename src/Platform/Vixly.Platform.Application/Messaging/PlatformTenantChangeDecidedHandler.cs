using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformTenantChangeDecidedHandler.cs
//
// PURPOSE:
// Emails the tenant owner the decision on its company change request (B12).
//
// RESPONSIBILITIES:
// - Send the approval or the rejection with the staff note to the owner contact
//
// IMPORTANT NOTES:
// - In the tenant’s default language
// =========================================================
public sealed class PlatformTenantChangeDecidedHandler : IPlatformOutboxHandler
{
    private readonly IPlatformTenantChangeRepository _requests;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformTenantMailer _mailer;

    public PlatformTenantChangeDecidedHandler(IPlatformTenantChangeRepository requests, IPlatformTenantRepository tenants, IPlatformTenantMailer mailer)
    {
        _requests = requests ?? throw new ArgumentNullException(nameof(requests));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
    }

    public string MessageType => PlatformMessageTypes.TenantCompanyChangeDecided;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformTenantCompanyChangeMessage decided = PlatformOutboxWriter.Read<PlatformTenantCompanyChangeMessage>(message);

        if (await _requests.GetAsync(decided.RequestId, cancellationToken) is not { } request
            || request.Status is not (PlatformTenantChangeStatus.Approved or PlatformTenantChangeStatus.Rejected)
            || await _tenants.GetAsync(request.PlatformTenantId, cancellationToken) is not PlatformTenant tenant)
        {
            return;
        }

        await _mailer.SendCompanyChangeDecidedAsync(tenant, tenant.ContactFor(PlatformTenantContactRole.Owner), request, cancellationToken);
    }
}
