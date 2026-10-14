using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.CreatePlatformPlan;

// =========================================================
// FILE: CreatePlatformPlanUseCase.cs
//
// PURPOSE:
// Creates a plan with its first draft version (D-S1, D-S2).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The code is unique: checked here, guaranteed by a unique index (a lost race reports code_taken)
// =========================================================
public sealed class CreatePlatformPlanUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly IPlatformPlanRepository _plans;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public CreatePlatformPlanUseCase(
        PlatformPlanAdministration administration,
        IPlatformPlanRepository plans,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CreatePlatformPlanResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CreatePlatformPlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _administration.AuthorizeAsync(context, PlatformPermissions.PlansManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return CreatePlatformPlanResponse.Failure(refused);
        }

        PlatformPlan plan;

        try
        {
            plan = PlatformPlan.Create(request.Code, request.Name, request.Description, request.CurrencyCode, context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return CreatePlatformPlanResponse.Failure(exception.Error);
        }

        if (await _plans.CodeExistsAsync(plan.Code, cancellationToken))
        {
            return CreatePlatformPlanResponse.Failure(PlatformPlanErrors.CodeTaken);
        }

        _plans.Add(plan);
        _administration.RecordChange(context, plan, PlatformPermissions.PlansManage, "platform.plans.created", after: new { code = plan.Code, currency = plan.CurrencyCode });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CreatePlatformPlanResponse.Success(plan.Id)
            : CreatePlatformPlanResponse.Failure(PlatformPlanErrors.CodeTaken);
    }
}
