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

namespace Vixly.Platform.Application.UseCases.Sales.ListPlatformPromotions;

// =========================================================
// FILE: ListPlatformPromotionsUseCase.cs
//
// PURPOSE:
// Pages the promotions for the console.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anyone who may see plans may see promotions
// =========================================================
public sealed class ListPlatformPromotionsUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformSubscriptionQueries _queries;

    public ListPlatformPromotionsUseCase(
        PlatformSalesAdministration sales,
        IPlatformSubscriptionQueries queries)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListPlatformPromotionsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformPromotionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.PlansView, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return ListPlatformPromotionsResponse.Failure(refused);
        }

        if (request.Status is { } status && !Enum.IsDefined(status))
        {
            return ListPlatformPromotionsResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        PagedResult<PlatformPromotionListItem> promotions = await _queries.ListPromotionsAsync(
            new PlatformPromotionListFilter(request.Status, search is { Length: > 100 } ? search[..100] : search, PlatformLanguageInput.Read(request.Language).Value),
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken);

        return ListPlatformPromotionsResponse.Success(
            promotions,
            new PlatformSalesAbilities(
                await _sales.HoldsAsync(context, PlatformPermissions.PromotionsManage, cancellationToken),
                await _sales.HoldsAsync(context, PlatformPermissions.PromotionsApprove, cancellationToken)));
    }
}
