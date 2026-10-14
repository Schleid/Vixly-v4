using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Plans.StartPlatformPlanDraft;

// =========================================================
// FILE: StartPlatformPlanDraftUseCase.cs
//
// PURPOSE:
// Starts a new draft from the current version of a plan (D-S2).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class StartPlatformPlanDraftUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public StartPlatformPlanDraftUseCase(
        PlatformPlanAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<StartPlatformPlanDraftResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        StartPlatformPlanDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPlanTarget target = await _administration.LoadPlanAsync(context, PlatformPermissions.PlansManage, request.PlatformPlanId, requireStepUp: false, cancellationToken);

        if (target.Plan is not PlatformPlan plan)
        {
            return StartPlatformPlanDraftResponse.Failure(target.Error!);
        }

        try
        {
            plan.StartDraft(_administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return StartPlatformPlanDraftResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, plan, PlatformPermissions.PlansManage, "platform.plans.draft_started");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? StartPlatformPlanDraftResponse.Success()
            : StartPlatformPlanDraftResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
