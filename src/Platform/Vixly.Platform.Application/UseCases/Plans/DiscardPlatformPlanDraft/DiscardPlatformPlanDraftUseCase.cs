using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Plans.DiscardPlatformPlanDraft;

// =========================================================
// FILE: DiscardPlatformPlanDraftUseCase.cs
//
// PURPOSE:
// Discards the draft of a plan that has a published version.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class DiscardPlatformPlanDraftUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public DiscardPlatformPlanDraftUseCase(
        PlatformPlanAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<DiscardPlatformPlanDraftResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        DiscardPlatformPlanDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPlanTarget target = await _administration.LoadPlanAsync(context, PlatformPermissions.PlansManage, request.PlatformPlanId, requireStepUp: false, cancellationToken);

        if (target.Plan is not PlatformPlan plan)
        {
            return DiscardPlatformPlanDraftResponse.Failure(target.Error!);
        }

        try
        {
            plan.DiscardDraft(_administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return DiscardPlatformPlanDraftResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, plan, PlatformPermissions.PlansManage, "platform.plans.draft_discarded");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? DiscardPlatformPlanDraftResponse.Success()
            : DiscardPlatformPlanDraftResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
