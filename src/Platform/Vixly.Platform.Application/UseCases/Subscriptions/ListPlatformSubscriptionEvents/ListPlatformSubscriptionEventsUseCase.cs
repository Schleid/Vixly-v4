using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ListPlatformSubscriptionEvents;

// =========================================================
// FILE: ListPlatformSubscriptionEventsUseCase.cs
//
// PURPOSE:
// Pages a tenant's subscription history, newest first.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListPlatformSubscriptionEventsUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformSubscriptionQueries _queries;

    public ListPlatformSubscriptionEventsUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformSubscriptionQueries queries)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListPlatformSubscriptionEventsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformSubscriptionEventsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _administration.AuthorizeAsync(context, PlatformPermissions.SubscriptionsView, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return ListPlatformSubscriptionEventsResponse.Failure(refused);
        }

        return ListPlatformSubscriptionEventsResponse.Success(await _queries.ListEventsAsync(
            request.PlatformTenantId,
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken));
    }
}
