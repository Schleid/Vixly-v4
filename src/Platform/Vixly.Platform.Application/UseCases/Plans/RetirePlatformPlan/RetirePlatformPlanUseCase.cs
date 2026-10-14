using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Plans.RetirePlatformPlan;

// =========================================================
// FILE: RetirePlatformPlanUseCase.cs
//
// PURPOSE:
// Retires a plan: no new subscriptions; subscribers stay on it.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class RetirePlatformPlanUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public RetirePlatformPlanUseCase(
        PlatformPlanAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RetirePlatformPlanResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RetirePlatformPlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPlanTarget target = await _administration.LoadPlanAsync(context, PlatformPermissions.PlansManage, request.PlatformPlanId, requireStepUp: false, cancellationToken);

        if (target.Plan is not PlatformPlan plan)
        {
            return RetirePlatformPlanResponse.Failure(target.Error!);
        }

        try
        {
            plan.Retire(_administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return RetirePlatformPlanResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, plan, PlatformPermissions.PlansManage, "platform.plans.retired");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RetirePlatformPlanResponse.Success()
            : RetirePlatformPlanResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
