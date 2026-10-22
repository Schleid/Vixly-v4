using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.ApprovePlatformTenantChange;

// =========================================================
// FILE: ApprovePlatformTenantChangeUseCase.cs
//
// PURPOSE:
// Lets staff approve a tenant’s company change request (B12).
//
// RESPONSIBILITIES:
// - Check who may act first, then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs platform.tenants.review_changes and a recent second factor
// - Applies the new details to the tenant and records it in the tenant history; issued invoices keep their details
// =========================================================
public sealed class ApprovePlatformTenantChangeUseCase
{
    private readonly PlatformTenantChanges _changes;

    public ApprovePlatformTenantChangeUseCase(
        PlatformTenantChanges changes)
    {
        _changes = changes ?? throw new ArgumentNullException(nameof(changes));
    }

    public async Task<ApprovePlatformTenantChangeResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ApprovePlatformTenantChangeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _changes.AuthorizeAsync(context, PlatformPermissions.TenantsReviewChanges, true, cancellationToken) is { } refused)
        {
            return ApprovePlatformTenantChangeResponse.Failure(refused);
        }

        PlatformTenantChangeRequest? change = await _changes.Requests.GetAsync(request.RequestId, cancellationToken);

        if (change is null)
        {
            return ApprovePlatformTenantChangeResponse.Failure(PlatformTenantChangeErrors.NotFound);
        }

        try
        {
            PlatformTenant? tenant = await _changes.Tenants.GetAsync(change.PlatformTenantId, cancellationToken);

            if (tenant is null)
            {
                return ApprovePlatformTenantChangeResponse.Failure(PlatformCommonErrors.NotFound);
            }

            _changes.Tenants.AddEvents(new[] { change.Approve(tenant, request.Note, context.PlatformUserId, _changes.UtcNow) });
        }
        catch (DomainException exception)
        {
            return ApprovePlatformTenantChangeResponse.Failure(exception.Error);
        }

        _changes.Record(context.PlatformUserId, "platform.tenants.company_change_approved", PlatformPermissions.TenantsReviewChanges, change,
            before: PlatformTenantChanges.Describe(change.Previous),
            after: PlatformTenantChanges.Describe(change.Proposed),
            arguments: new { note = change.DecisionNote });
        _changes.Notify(Vixly.Platform.Application.Messaging.PlatformMessageTypes.TenantCompanyChangeDecided, change);

        return await _changes.SaveAsync(cancellationToken)
            ? ApprovePlatformTenantChangeResponse.Success()
            : ApprovePlatformTenantChangeResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
