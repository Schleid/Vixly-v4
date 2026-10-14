using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Plans.ListPlatformPlans;

// =========================================================
// FILE: ListPlatformPlansUseCase.cs
//
// PURPOSE:
// Pages the plans for the console.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListPlatformPlansUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly IPlatformSubscriptionQueries _queries;

    public ListPlatformPlansUseCase(
        PlatformPlanAdministration administration,
        IPlatformSubscriptionQueries queries)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListPlatformPlansResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformPlansRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _administration.AuthorizeAsync(context, PlatformPermissions.PlansView, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return ListPlatformPlansResponse.Failure(refused);
        }

        if (request.Status is { } status && !Enum.IsDefined(status))
        {
            return ListPlatformPlansResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        if (search is { Length: > 100 })
        {
            search = search[..100];
        }

        PagedResult<PlatformPlanListItem> plans = await _queries.ListPlansAsync(
            new PlatformPlanListFilter(request.Status, search, PlatformLanguageInput.Read(request.Language).Value),
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken);

        return ListPlatformPlansResponse.Success(plans, await _administration.ReadAbilitiesAsync(context, cancellationToken));
    }
}
