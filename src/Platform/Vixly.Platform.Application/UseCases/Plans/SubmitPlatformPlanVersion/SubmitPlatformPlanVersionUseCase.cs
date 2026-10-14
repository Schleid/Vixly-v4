using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Plans.SubmitPlatformPlanVersion;

// =========================================================
// FILE: SubmitPlatformPlanVersionUseCase.cs
//
// PURPOSE:
// Submits a plan's draft version for CFO approval (D-S26).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The CFO is asked by email; the version goes live only when approved by someone else
// =========================================================
public sealed class SubmitPlatformPlanVersionUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public SubmitPlatformPlanVersionUseCase(
        PlatformPlanAdministration administration,
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SubmitPlatformPlanVersionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SubmitPlatformPlanVersionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPlanTarget target = await _administration.LoadPlanAsync(context, PlatformPermissions.PlansManage, request.PlatformPlanId, requireStepUp: false, cancellationToken);

        if (target.Plan is not PlatformPlan plan)
        {
            return SubmitPlatformPlanVersionResponse.Failure(target.Error!);
        }

        PlatformPlanVersion submitted;

        try
        {
            submitted = plan.SubmitDraft(context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return SubmitPlatformPlanVersionResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, plan, PlatformPermissions.PlansManage, "platform.plans.version_submitted", after: new { version = submitted.Number });
        _sales.RequestApproval(PlatformApprovalKind.PlanVersion, plan.Id, PlatformPermissions.PlansPublish, plan.Code + " v" + submitted.Number.ToString(System.Globalization.CultureInfo.InvariantCulture), context.PlatformUserId);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SubmitPlatformPlanVersionResponse.Success()
            : SubmitPlatformPlanVersionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
