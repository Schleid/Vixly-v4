using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.Plans.GetPlatformPlan;

// =========================================================
// FILE: GetPlatformPlanUseCase.cs
//
// PURPOSE:
// Reads one plan for the plan page.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformPlanUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly IPlatformSubscriptionQueries _queries;

    public GetPlatformPlanUseCase(
        PlatformPlanAdministration administration,
        IPlatformSubscriptionQueries queries)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<GetPlatformPlanResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformPlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPlanTarget target = await _administration.LoadPlanAsync(context, PlatformPermissions.PlansView, request.PlatformPlanId, requireStepUp: false, cancellationToken);

        if (target.Plan is not PlatformPlan plan)
        {
            return GetPlatformPlanResponse.Failure(target.Error!);
        }

        LanguageCode language = PlatformLanguageInput.Read(request.Language);
        IReadOnlyDictionary<Guid, string> names = await _queries.StaffNamesAsync(
            plan.Versions.Where(version => version.PublishedByUserId is not null).Select(version => version.PublishedByUserId!.Value),
            cancellationToken);
        PlatformPlanVersionItem Item(PlatformPlanVersion version) => PlatformPlanVersionItem.From(
            version,
            version.PublishedByUserId is { } by && names.TryGetValue(by, out string? name) ? name : null);

        PlatformPlanDetail detail = new(
            plan.Id,
            plan.Code,
            plan.NameIn(language),
            plan.Status,
            plan.IsPublic,
            plan.DisplayOrder,
            plan.Translations
                .OrderBy(item => LanguageCode.All.ToList().IndexOf(item.Language))
                .Select(item => new PlatformPlanTranslationItem(item.Language.Value, item.Name, item.Description))
                .ToArray(),
            plan.Current is null ? null : Item(plan.Current),
            plan.Draft is null ? null : Item(plan.Draft),
            plan.Versions.OrderByDescending(version => version.Number).Select(Item).ToArray(),
            await _queries.CountSubscribersAsync(plan.Id, cancellationToken),
            plan.CreatedUtc);

        PlatformPlanAbilities abilities = await _administration.ReadAbilitiesAsync(context, cancellationToken);
        bool decide = abilities.CanPublish && plan.Draft is { IsAwaitingApproval: true } draft && draft.SubmittedByUserId != context.PlatformUserId;

        return GetPlatformPlanResponse.Success(detail, abilities with { CanDecide = decide });
    }
}
