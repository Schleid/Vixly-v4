using Vixly.Platform.Application.Queries;
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

    public ListPlatformSubscriptionsUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformSubscriptionQueries queries)
    {
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

        return ListPlatformSubscriptionsResponse.Success(await _queries.ListSubscriptionsAsync(
            new PlatformSubscriptionListFilter(
                request.Status,
                request.PlatformPlanId,
                renewsBefore,
                search,
                request.IncludeEnded || request.Status == PlatformSubscriptionStatus.Ended,
                request.DiscountedOnly,
                PlatformLanguageInput.Read(request.Language).Value),
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken));
    }
}
