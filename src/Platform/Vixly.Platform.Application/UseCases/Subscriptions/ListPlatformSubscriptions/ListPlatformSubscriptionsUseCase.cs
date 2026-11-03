using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ListPlatformSubscriptions;

// =========================================================
// FILE: ListPlatformSubscriptionsUseCase.cs
//
// PURPOSE:
// Pages every tenant subscription for the subscriptions screen.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListPlatformSubscriptionsUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformSubscriptionQueries _queries;

    private readonly IPlatformShipmentUsageRepository _usage;

    public ListPlatformSubscriptionsUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformSubscriptionQueries queries,
        IPlatformShipmentUsageRepository usage)
    {
        _usage = usage ?? throw new ArgumentNullException(nameof(usage));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListPlatformSubscriptionsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformSubscriptionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _administration.AuthorizeAsync(context, PlatformPermissions.SubscriptionsView, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return ListPlatformSubscriptionsResponse.Failure(refused);
        }

        if ((request.Status is { } status && !Enum.IsDefined(status)) || request.RenewsWithinDays is < 0 or > 366)
        {
            return ListPlatformSubscriptionsResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        if (search is { Length: > 100 })
        {
            search = search[..100];
        }

        DateTimeOffset? renewsBefore = request.RenewsWithinDays is int days ? _administration.UtcNow.AddDays(days) : null;

        PagedResult<PlatformSubscriptionListItem> page = await _queries.ListSubscriptionsAsync(
            new PlatformSubscriptionListFilter(
                request.Status,
                request.PlatformPlanId,
                renewsBefore,
                search,
                request.IncludeEnded || request.Status == PlatformSubscriptionStatus.Ended,
                request.DiscountedOnly,
                PlatformLanguageInput.Read(request.Language).Value),
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken);

        // Shipment cap (owner, 2026-10-29): each company's latest monthly shipments, next to its plan.
        Dictionary<Guid, PlatformTenantShipmentUsage> usage = (await _usage.ListLatestAsync(page.Items.Select(item => item.PlatformTenantId).Distinct().ToArray(), cancellationToken))
            .ToDictionary(item => item.PlatformTenantId);

        return ListPlatformSubscriptionsResponse.Success(new PagedResult<PlatformSubscriptionListItem>(
            page.Items.Select(item => usage.TryGetValue(item.PlatformTenantId, out PlatformTenantShipmentUsage? month) ? item with { Shipments = month.Shipments, ShipmentsMonth = month.Month } : item).ToArray(),
            PagedRequest.Create(page.Page, page.PageSize),
            page.TotalCount));
    }
}
