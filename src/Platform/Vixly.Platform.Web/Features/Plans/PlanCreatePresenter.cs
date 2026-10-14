using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Plans.CreatePlatformPlan;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Plans;

// =========================================================
// FILE: PlanCreatePresenter.cs
//
// PURPOSE:
// Behaviour of the new plan form.
//
// RESPONSIBILITIES:
// - Create a plan with its first draft and report its id
//
// IMPORTANT NOTES:
// - The draft is then edited on the plan page
// =========================================================
public sealed class PlanCreatePresenter : PlatformPresenter<PlanCreateState>
{
    private readonly CreatePlatformPlanUseCase _create;

    public PlanCreatePresenter(CreatePlatformPlanUseCase create, PlatformUseCaseScope scope, ILogger<PlanCreatePresenter> logger)
        : base(scope, PlanCreateState.Initial, logger)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
    }

    public async Task<Guid?> CreateAsync(string? code, string? name, string? description, string? currencyCode)
    {
        CreatePlatformPlanResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _create.ExecuteAsync(
                context,
                new CreatePlatformPlanRequest { Code = code ?? string.Empty, Name = name ?? string.Empty, Description = description, CurrencyCode = currencyCode ?? string.Empty },
                cancellationToken);
            return response.Error;
        });

        SetState(new PlanCreateState(error is null ? response?.PlatformPlanId : null, error));

        return error is null ? response?.PlatformPlanId : null;
    }
}
