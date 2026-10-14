using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Sales.ListPlatformApprovals;

// =========================================================
// FILE: ListPlatformApprovalsUseCase.cs
//
// PURPOSE:
// Lists what waits for the current staff member's approval (D-S24, D-S26).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Shows only the kinds the person may approve, and never their own requests (four eyes)
// =========================================================
public sealed class ListPlatformApprovalsUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformSubscriptionQueries _queries;

    public ListPlatformApprovalsUseCase(
        PlatformSalesAdministration sales,
        IPlatformSubscriptionQueries queries)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListPlatformApprovalsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformApprovalsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        List<PlatformApprovalKind> kinds = new();

        if (await _sales.HoldsAsync(context, PlatformPermissions.DiscountsApprove, cancellationToken))
        {
            kinds.Add(PlatformApprovalKind.Discount);
            kinds.Add(PlatformApprovalKind.Offer);
        }

        if (await _sales.HoldsAsync(context, PlatformPermissions.PromotionsApprove, cancellationToken))
        {
            kinds.Add(PlatformApprovalKind.Promotion);
        }

        if (await _sales.HoldsAsync(context, PlatformPermissions.PlansPublish, cancellationToken))
        {
            kinds.Add(PlatformApprovalKind.PlanVersion);
        }

        if (kinds.Count == 0)
        {
            Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.DiscountsApprove, requireStepUp: false, cancellationToken);

            return ListPlatformApprovalsResponse.Failure(refused ?? PlatformCommonErrors.NotFound);
        }

        IReadOnlyList<PlatformApprovalItem> items = await _queries.ListApprovalsAsync(kinds, PlatformLanguageInput.Read(request.Language).Value, cancellationToken);

        return ListPlatformApprovalsResponse.Success(items.Where(item => item.RequestedByUserId != context.PlatformUserId).ToArray());
    }
}
