using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenant;

// =========================================================
// FILE: GetPlatformTenantUseCase.cs
//
// PURPOSE:
// Reads one tenant for its page.
//
// RESPONSIBILITIES:
// - Check platform.tenants.view
// - Return the tenant and what the actor may do to it
//
// IMPORTANT NOTES:
// - History and notes are read by their own paged use cases
// =========================================================
public sealed class GetPlatformTenantUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformTenantQueries _queries;

    private readonly PlatformTenantAdministration _administration;

    public GetPlatformTenantUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformTenantQueries queries,
        PlatformTenantAdministration administration)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
    }

    public async Task<GetPlatformTenantResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.TenantsView, cancellationToken);

        PlatformTenantDetail? tenant = await _queries.GetAsync(request.PlatformTenantId, cancellationToken);

        if (tenant is null)
        {
            return GetPlatformTenantResponse.Failure(PlatformCommonErrors.NotFound);
        }

        return GetPlatformTenantResponse.Success(tenant, await _administration.ReadAbilitiesAsync(context, cancellationToken));
    }
}
