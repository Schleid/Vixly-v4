using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Tenants.ListAccountManagerCandidates;

// =========================================================
// FILE: ListAccountManagerCandidatesUseCase.cs
//
// PURPOSE:
// Lists the active staff who can be made account manager.
//
// RESPONSIBILITIES:
// - Check platform.tenants.edit
// - Return names only
//
// IMPORTANT NOTES:
// - Lets the tenant page offer a choice without users.view
// =========================================================
public sealed class ListAccountManagerCandidatesUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformTenantQueries _queries;

    public ListAccountManagerCandidatesUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformTenantQueries queries)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListAccountManagerCandidatesResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListAccountManagerCandidatesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.TenantsEdit, cancellationToken);

        return ListAccountManagerCandidatesResponse.Success(await _queries.ListActiveStaffAsync(cancellationToken));
    }
}
