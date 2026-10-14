using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Registrations.ListPlatformTenantRegistrations;

// =========================================================
// FILE: ListPlatformTenantRegistrationsUseCase.cs
//
// PURPOSE:
// Lists self-service registrations for staff, paged.
//
// RESPONSIBILITIES:
// - Check platform.tenants.view
// - Filter by status and search name, email or slug
//
// IMPORTANT NOTES:
// - Paged (P-1)
// =========================================================
public sealed class ListPlatformTenantRegistrationsUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformTenantQueries _queries;

    public ListPlatformTenantRegistrationsUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformTenantQueries queries)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListPlatformTenantRegistrationsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformTenantRegistrationsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.TenantsView, cancellationToken);

        if (request.Status is { } status && !Enum.IsDefined(status))
        {
            return ListPlatformTenantRegistrationsResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        PagedResult<PlatformTenantRegistrationListItem> registrations = await _queries.ListRegistrationsAsync(
            request.Status,
            search is { Length: > 100 } ? search[..100] : search,
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken);

        return ListPlatformTenantRegistrationsResponse.Success(registrations);
    }
}
