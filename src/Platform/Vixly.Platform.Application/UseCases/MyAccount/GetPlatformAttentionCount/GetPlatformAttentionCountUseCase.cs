using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.MyAccount.GetPlatformAttentionCount;

// =========================================================
// FILE: GetPlatformAttentionCountUseCase.cs
//
// PURPOSE:
// Counts what waits on one page, for the badge beside its menu entry (owner, 2026-11-04).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Checks the page's own permission without refusing: no permission, no number (and nothing written to the audit)
// - Approvals never count the person's own requests (four eyes); Zelle counts waiting reports; company changes count pending ones
// =========================================================
public sealed class GetPlatformAttentionCountUseCase
{
    private static readonly PagedRequest One = PagedRequest.Create(1, 1);

    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformSubscriptionQueries _queries;

    private readonly PlatformZelleAdministration _zelle;

    private readonly PlatformTenantChanges _changes;

    public GetPlatformAttentionCountUseCase(
        PlatformSalesAdministration sales,
        IPlatformSubscriptionQueries queries,
        PlatformZelleAdministration zelle,
        PlatformTenantChanges changes)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
        _changes = changes ?? throw new ArgumentNullException(nameof(changes));
    }

    public async Task<GetPlatformAttentionCountResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformAttentionCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        switch (request.Attention)
        {
            case PlatformAttention.Approvals:
                IReadOnlyList<PlatformApprovalKind> kinds = await PlatformApprovalKinds.ForAsync(_sales, context, cancellationToken);

                if (kinds.Count == 0)
                {
                    return GetPlatformAttentionCountResponse.Success(null);
                }

                IReadOnlyList<PlatformApprovalItem> items = await _queries.ListApprovalsAsync(kinds, "en", cancellationToken);
                return GetPlatformAttentionCountResponse.Success(items.Count(item => item.RequestedByUserId != context.PlatformUserId));
            case PlatformAttention.Zelle:
                return await _zelle.HoldsAsync(context, PlatformPermissions.BillingView, cancellationToken)
                    ? GetPlatformAttentionCountResponse.Success((await _zelle.Queries.ListClaimsAsync(
                        new PlatformZelleClaimFilter(Vixly.Platform.Domain.Payments.PlatformZelleClaimStatus.Waiting, null, null), One, cancellationToken)).TotalCount)
                    : GetPlatformAttentionCountResponse.Success(null);
            case PlatformAttention.TenantChanges:
                return await _sales.HoldsAsync(context, PlatformPermissions.TenantsView, cancellationToken)
                    ? GetPlatformAttentionCountResponse.Success((await _changes.Requests.ListAsync(
                        new PlatformTenantChangeFilter(Vixly.Platform.Domain.Tenants.PlatformTenantChangeStatus.Pending, null), One, cancellationToken)).TotalCount)
                    : GetPlatformAttentionCountResponse.Success(null);
            default:
                return GetPlatformAttentionCountResponse.Success(null);
        }
    }
}
