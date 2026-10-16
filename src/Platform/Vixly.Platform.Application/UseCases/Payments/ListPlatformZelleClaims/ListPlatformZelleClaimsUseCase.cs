using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.ListPlatformZelleClaims;

// =========================================================
// FILE: ListPlatformZelleClaimsUseCase.cs
//
// PURPOSE:
// Lists Zelle reports, paged; the waiting ones are staff's to-do list.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListPlatformZelleClaimsUseCase
{
    private readonly PlatformZelleAdministration _zelle;

    public ListPlatformZelleClaimsUseCase(
        PlatformZelleAdministration zelle)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
    }

    public async Task<ListPlatformZelleClaimsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformZelleClaimsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _zelle.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return ListPlatformZelleClaimsResponse.Failure(refused);
        }

        return ListPlatformZelleClaimsResponse.Success(await _zelle.Queries.ListClaimsAsync(
            new PlatformZelleClaimFilter(request.Status, request.PlatformTenantId, string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim()),
            Vixly.Shared.Kernel.Paging.PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken));
    }
}
