using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Plans.UpdatePlatformPlanDraft;

// =========================================================
// FILE: UpdatePlatformPlanDraftUseCase.cs
//
// PURPOSE:
// Edits the draft version of a plan (D-S2, D-S7).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only the draft is editable; published versions never change
// =========================================================
public sealed class UpdatePlatformPlanDraftUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public UpdatePlatformPlanDraftUseCase(
        PlatformPlanAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdatePlatformPlanDraftResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformPlanDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPlanTarget target = await _administration.LoadPlanAsync(context, PlatformPermissions.PlansManage, request.PlatformPlanId, requireStepUp: false, cancellationToken);

        if (target.Plan is not PlatformPlan plan)
        {
            return UpdatePlatformPlanDraftResponse.Failure(target.Error!);
        }

        try
        {
            plan.EditDraft(
                PlatformPlanVersionTerms.Create(
                    request.CurrencyCode,
                    request.MonthlyPricePerLicence,
                    request.AnnualPricePerLicence,
                    request.SetupFee,
                    request.MinimumLicences,
                    request.TrialDays,
                    request.MaxLicensedDevices,
                    request.MaxWarehouses,
                    request.Features),
                _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformPlanDraftResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, plan, PlatformPermissions.PlansManage, "platform.plans.draft_updated", after: new { monthly = request.MonthlyPricePerLicence, annual = request.AnnualPricePerLicence, setup = request.SetupFee, trial = request.TrialDays });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UpdatePlatformPlanDraftResponse.Success()
            : UpdatePlatformPlanDraftResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
