using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantCompanyChange;

// =========================================================
// FILE: GetTenantCompanyChangeUseCase.cs
//
// PURPOSE:
// Shows the tenant owner or an admin the latest company change request and its decision (B12).
//
// RESPONSIBILITIES:
// - Check who may act first, then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The staff decision note is shown to the tenant; it is written for them
// =========================================================
public sealed class GetTenantCompanyChangeUseCase
{
    private readonly PlatformTenantChanges _changes;

    public GetTenantCompanyChangeUseCase(
        PlatformTenantChanges changes)
    {
        _changes = changes ?? throw new ArgumentNullException(nameof(changes));
    }

    public async Task<GetTenantCompanyChangeResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        GetTenantCompanyChangeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? notAllowed = PlatformTenantAccess.Require(context, false, PlatformTenantActorRole.Owner, PlatformTenantActorRole.Admin);

        return notAllowed is not null
            ? GetTenantCompanyChangeResponse.Failure(notAllowed)
            : GetTenantCompanyChangeResponse.Success(await _changes.Requests.GetLatestItemAsync(context.PlatformTenantId, cancellationToken));
    }
}
