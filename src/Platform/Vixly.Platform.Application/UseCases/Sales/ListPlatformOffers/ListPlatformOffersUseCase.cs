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

namespace Vixly.Platform.Application.UseCases.Sales.ListPlatformOffers;

// =========================================================
// FILE: ListPlatformOffersUseCase.cs
//
// PURPOSE:
// Pages the offer links for the console.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Open to people who prepare offers and to the sales manager who approves them
// =========================================================
public sealed class ListPlatformOffersUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformSubscriptionQueries _queries;

    public ListPlatformOffersUseCase(
        PlatformSalesAdministration sales,
        IPlatformSubscriptionQueries queries)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListPlatformOffersResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformOffersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // Open to whoever prepares offers or approves them; the gate refuses anyone else.
        if (!await _sales.HoldsAsync(context, PlatformPermissions.OffersManage, cancellationToken)
            && await _sales.AuthorizeAsync(context, PlatformPermissions.DiscountsApprove, requireStepUp: false, cancellationToken) is { } refused)
        {
            return ListPlatformOffersResponse.Failure(refused);
        }

        if (request.Status is { } status && !Enum.IsDefined(status))
        {
            return ListPlatformOffersResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        PagedResult<PlatformOfferListItem> offers = await _queries.ListOffersAsync(
            new PlatformOfferListFilter(request.Status, search is { Length: > 100 } ? search[..100] : search, PlatformLanguageInput.Read(request.Language).Value),
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken);

        return ListPlatformOffersResponse.Success(
            offers,
            new PlatformSalesAbilities(
                await _sales.HoldsAsync(context, PlatformPermissions.OffersManage, cancellationToken),
                await _sales.HoldsAsync(context, PlatformPermissions.DiscountsApprove, cancellationToken)));
    }
}
