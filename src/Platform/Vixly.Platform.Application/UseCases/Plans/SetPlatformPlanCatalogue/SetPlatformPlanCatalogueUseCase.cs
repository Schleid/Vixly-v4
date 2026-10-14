using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Plans.SetPlatformPlanCatalogue;

// =========================================================
// FILE: SetPlatformPlanCatalogueUseCase.cs
//
// PURPOSE:
// Sets whether a plan shows in the public catalogue, and its order.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SetPlatformPlanCatalogueUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public SetPlatformPlanCatalogueUseCase(
        PlatformPlanAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SetPlatformPlanCatalogueResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SetPlatformPlanCatalogueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPlanTarget target = await _administration.LoadPlanAsync(context, PlatformPermissions.PlansManage, request.PlatformPlanId, requireStepUp: false, cancellationToken);

        if (target.Plan is not PlatformPlan plan)
        {
            return SetPlatformPlanCatalogueResponse.Failure(target.Error!);
        }

        try
        {
            plan.SetCatalogue(request.IsPublic, request.DisplayOrder, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return SetPlatformPlanCatalogueResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, plan, PlatformPermissions.PlansManage, "platform.plans.catalogue_set", after: new { isPublic = request.IsPublic, order = request.DisplayOrder });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SetPlatformPlanCatalogueResponse.Success()
            : SetPlatformPlanCatalogueResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
