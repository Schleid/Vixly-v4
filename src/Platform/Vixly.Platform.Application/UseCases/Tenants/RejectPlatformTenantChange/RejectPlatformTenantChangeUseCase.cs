using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.RejectPlatformTenantChange;

// =========================================================
// FILE: RejectPlatformTenantChangeUseCase.cs
//
// PURPOSE:
// Lets staff reject a tenant’s company change request (B12).
//
// RESPONSIBILITIES:
// - Check who may act first, then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs platform.tenants.review_changes and a recent second factor
// - The note is required and is emailed to the tenant owner
// =========================================================
public sealed class RejectPlatformTenantChangeUseCase
{
    private readonly PlatformTenantChanges _changes;

    public RejectPlatformTenantChangeUseCase(
        PlatformTenantChanges changes)
    {
        _changes = changes ?? throw new ArgumentNullException(nameof(changes));
    }

    public async Task<RejectPlatformTenantChangeResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RejectPlatformTenantChangeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _changes.AuthorizeAsync(context, PlatformPermissions.TenantsReviewChanges, true, cancellationToken) is { } refused)
        {
            return RejectPlatformTenantChangeResponse.Failure(refused);
        }

        PlatformTenantChangeRequest? change = await _changes.Requests.GetAsync(request.RequestId, cancellationToken);

        if (change is null)
        {
            return RejectPlatformTenantChangeResponse.Failure(PlatformTenantChangeErrors.NotFound);
        }

        try
        {
            change.Reject(request.Note, context.PlatformUserId, _changes.UtcNow);
        }
        catch (DomainException exception)
        {
            return RejectPlatformTenantChangeResponse.Failure(exception.Error);
        }

        _changes.Record(context.PlatformUserId, "platform.tenants.company_change_rejected", PlatformPermissions.TenantsReviewChanges, change,
            before: PlatformTenantChanges.Describe(change.Previous),
            after: PlatformTenantChanges.Describe(change.Proposed),
            arguments: new { note = change.DecisionNote });
        _changes.Notify(Vixly.Platform.Application.Messaging.PlatformMessageTypes.TenantCompanyChangeDecided, change);

        return await _changes.SaveAsync(cancellationToken)
            ? RejectPlatformTenantChangeResponse.Success()
            : RejectPlatformTenantChangeResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
