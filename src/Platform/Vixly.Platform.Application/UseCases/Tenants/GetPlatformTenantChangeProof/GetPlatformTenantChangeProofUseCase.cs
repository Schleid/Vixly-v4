using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenantChangeProof;

// =========================================================
// FILE: GetPlatformTenantChangeProofUseCase.cs
//
// PURPOSE:
// Lets staff open the proof document of a change request (B12).
//
// RESPONSIBILITIES:
// - Check who may act first, then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The proof may hold personal data: the download is audited by the web host’s request log only
// =========================================================
public sealed class GetPlatformTenantChangeProofUseCase
{
    private readonly PlatformTenantChanges _changes;

    public GetPlatformTenantChangeProofUseCase(
        PlatformTenantChanges changes)
    {
        _changes = changes ?? throw new ArgumentNullException(nameof(changes));
    }

    public async Task<GetPlatformTenantChangeProofResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformTenantChangeProofRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _changes.AuthorizeAsync(context, PlatformPermissions.TenantsView, false, cancellationToken);
        PlatformTenantChangeProofFile? file = await _changes.Requests.GetProofAsync(request.RequestId, cancellationToken);

        return file is null
            ? GetPlatformTenantChangeProofResponse.Failure(PlatformTenantChangeErrors.NotFound)
            : GetPlatformTenantChangeProofResponse.Success(file);
    }
}
