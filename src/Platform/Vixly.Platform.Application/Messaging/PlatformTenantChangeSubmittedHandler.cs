using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformTenantChangeSubmittedHandler.cs
//
// PURPOSE:
// Emails the staff who review company changes when a tenant submits one (B12).
//
// RESPONSIBILITIES:
// - Find every holder of platform.tenants.review_changes and email each once
//
// IMPORTANT NOTES:
// - A request already decided or withdrawn sends nothing
// =========================================================
public sealed class PlatformTenantChangeSubmittedHandler : IPlatformOutboxHandler
{
    private readonly IPlatformTenantChangeRepository _requests;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformRoleQueries _roles;

    private readonly IPlatformTenantMailer _mailer;

    private readonly TimeProvider _timeProvider;

    public PlatformTenantChangeSubmittedHandler(IPlatformTenantChangeRepository requests, IPlatformTenantRepository tenants, IPlatformRoleQueries roles, IPlatformTenantMailer mailer, TimeProvider timeProvider)
    {
        _requests = requests ?? throw new ArgumentNullException(nameof(requests));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public string MessageType => PlatformMessageTypes.TenantCompanyChangeSubmitted;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformTenantCompanyChangeMessage submitted = PlatformOutboxWriter.Read<PlatformTenantCompanyChangeMessage>(message);

        if (await _requests.GetAsync(submitted.RequestId, cancellationToken) is not { IsPending: true } request
            || await _tenants.GetAsync(request.PlatformTenantId, cancellationToken) is not PlatformTenant tenant)
        {
            return;
        }

        foreach (PlatformPermissionHolderItem holder in (await _roles.ListPermissionHoldersAsync(PlatformPermissions.TenantsReviewChanges, _timeProvider.GetUtcNow(), cancellationToken)).DistinctBy(item => item.PlatformUserId))
        {
            await _mailer.SendCompanyChangeSubmittedAsync(holder, tenant, request, cancellationToken);
        }
    }
}
