using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Plans.ReactivatePlatformPlan;

// =========================================================
// FILE: ReactivatePlatformPlanUseCase.cs
//
// PURPOSE:
// Opens a retired plan to new subscriptions again.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ReactivatePlatformPlanUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ReactivatePlatformPlanUseCase(
        PlatformPlanAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ReactivatePlatformPlanResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ReactivatePlatformPlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPlanTarget target = await _administration.LoadPlanAsync(context, PlatformPermissions.PlansManage, request.PlatformPlanId, requireStepUp: false, cancellationToken);

        if (target.Plan is not PlatformPlan plan)
        {
            return ReactivatePlatformPlanResponse.Failure(target.Error!);
        }

        try
        {
            plan.Reactivate(_administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return ReactivatePlatformPlanResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, plan, PlatformPermissions.PlansManage, "platform.plans.reactivated");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ReactivatePlatformPlanResponse.Success()
            : ReactivatePlatformPlanResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
