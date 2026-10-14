using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.Plans.SetPlatformPlanTranslation;

// =========================================================
// FILE: SetPlatformPlanTranslationUseCase.cs
//
// PURPOSE:
// Sets the name and description of a plan in one language.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - An empty name and description in French or Spanish removes that translation; English is required
// =========================================================
public sealed class SetPlatformPlanTranslationUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public SetPlatformPlanTranslationUseCase(
        PlatformPlanAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SetPlatformPlanTranslationResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SetPlatformPlanTranslationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPlanTarget target = await _administration.LoadPlanAsync(context, PlatformPermissions.PlansManage, request.PlatformPlanId, requireStepUp: false, cancellationToken);

        if (target.Plan is not PlatformPlan plan)
        {
            return SetPlatformPlanTranslationResponse.Failure(target.Error!);
        }

        if (!LanguageCode.TryCreate(request.Language, out LanguageCode? language))
        {
            return SetPlatformPlanTranslationResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        try
        {
            plan.SetTranslation(language, request.Name, request.Description, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return SetPlatformPlanTranslationResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, plan, PlatformPermissions.PlansManage, "platform.plans.translation_set", after: new { language = language.Value });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SetPlatformPlanTranslationResponse.Success()
            : SetPlatformPlanTranslationResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
