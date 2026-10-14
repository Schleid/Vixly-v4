using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Plans.RejectPlatformPlanVersion;

// =========================================================
// FILE: RejectPlatformPlanVersionUseCase.cs
//
// PURPOSE:
// Sends a submitted plan version back to sales with a reason (D-S26).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs step-up, like approval
// - The version returns to Draft with the reason shown to sales
// =========================================================
public sealed class RejectPlatformPlanVersionUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public RejectPlatformPlanVersionUseCase(
        PlatformPlanAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RejectPlatformPlanVersionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RejectPlatformPlanVersionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPlanTarget target = await _administration.LoadPlanAsync(context, PlatformPermissions.PlansPublish, request.PlatformPlanId, requireStepUp: true, cancellationToken);

        if (target.Plan is not PlatformPlan plan)
        {
            return RejectPlatformPlanVersionResponse.Failure(target.Error!);
        }

        PlatformPlanVersion rejected;

        try
        {
            rejected = plan.RejectDraft(context.PlatformUserId, request.Reason, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return RejectPlatformPlanVersionResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, plan, PlatformPermissions.PlansPublish, "platform.plans.version_rejected", after: new { version = rejected.Number }, reason: rejected.RejectionReason);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RejectPlatformPlanVersionResponse.Success()
            : RejectPlatformPlanVersionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
