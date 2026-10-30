using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenantChange;

// =========================================================
// FILE: GetPlatformTenantChangeUseCase.cs
//
// PURPOSE:
// Shows one company change request to staff (B12).
//
// RESPONSIBILITIES:
// - Check who may act first, then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformTenantChangeUseCase
{
    private readonly PlatformTenantChanges _changes;

    public GetPlatformTenantChangeUseCase(
        PlatformTenantChanges changes)
    {
        _changes = changes ?? throw new ArgumentNullException(nameof(changes));
    }

    public async Task<GetPlatformTenantChangeResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformTenantChangeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _changes.AuthorizeAsync(context, PlatformPermissions.TenantsView, false, cancellationToken);
        PlatformTenantChangeItem? change = await _changes.Requests.GetItemAsync(request.RequestId, cancellationToken);

        return change is null
            ? GetPlatformTenantChangeResponse.Failure(PlatformTenantChangeErrors.NotFound)
            : GetPlatformTenantChangeResponse.Success(change, change.Status == PlatformTenantChangeStatus.Pending && await _changes.CanReviewAsync(context, cancellationToken));
    }
}
