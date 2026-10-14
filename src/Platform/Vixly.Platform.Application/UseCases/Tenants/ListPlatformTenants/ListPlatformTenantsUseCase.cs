using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenants;

// =========================================================
// FILE: ListPlatformTenantsUseCase.cs
//
// PURPOSE:
// Lists tenants for the directory, paged and filtered (P-1).
//
// RESPONSIBILITIES:
// - Check platform.tenants.view
// - Trim the search and validate the status filter
// - Say whether the actor may create tenants
//
// IMPORTANT NOTES:
// - V3 loaded every tenant; this reads one page
// - Search covers name, code, slug, email and phone (V3's
//   SearchTenantByPhone is folded in)
// =========================================================
public sealed class ListPlatformTenantsUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformTenantQueries _queries;

    private readonly IPlatformPermissionReader _permissions;

    private readonly TimeProvider _timeProvider;

    public ListPlatformTenantsUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformTenantQueries queries,
        IPlatformPermissionReader permissions,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListPlatformTenantsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformTenantsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.TenantsView, cancellationToken);

        if (request.Status is { } status && !Enum.IsDefined(status))
        {
            return ListPlatformTenantsResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        string? search = Trim(request.Search, 100);
        string? tag = Trim(request.Tag, PlatformTenantLimits.TagMaxLength)?.ToLowerInvariant();

        PagedResult<PlatformTenantListItem> tenants = await _queries.ListAsync(
            new PlatformTenantListFilter(search, request.Status, tag, request.AccountManagerUserId),
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken);
        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(context.PlatformUserId, _timeProvider.GetUtcNow(), cancellationToken);

        return ListPlatformTenantsResponse.Success(tenants, PlatformAccess.Holds(actor, PlatformPermissions.TenantsCreate));
    }

    private static string? Trim(string? value, int maxLength)
    {
        string? trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        return trimmed is { Length: > 0 } && trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
