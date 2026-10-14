using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Plans.PublishPlatformPlanVersion;
using Vixly.Platform.Application.UseCases.Plans.RejectPlatformPlanVersion;
using Vixly.Platform.Web.Features.Sales;

namespace Vixly.Platform.Web.Features.Plans;

// =========================================================
// FILE: PlanApprovalPresenter.cs
//
// PURPOSE:
// The CFO's decision on a submitted plan version (D-S26).
//
// RESPONSIBILITIES:
// - Approve and publish, or reject with a reason, with step-up
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// =========================================================
public sealed class PlanApprovalPresenter : SalesActionPresenter
{
    private readonly PublishPlatformPlanVersionUseCase _publish;

    private readonly RejectPlatformPlanVersionUseCase _reject;

    public PlanApprovalPresenter(PublishPlatformPlanVersionUseCase publish, RejectPlatformPlanVersionUseCase reject, PlatformUseCaseScope scope, ILogger<PlanApprovalPresenter> logger)
        : base(scope, logger)
    {
        _publish = publish ?? throw new ArgumentNullException(nameof(publish));
        _reject = reject ?? throw new ArgumentNullException(nameof(reject));
    }

    public Task<bool> ApproveAsync(Guid platformPlanId)
    {
        return ActAsync("ui.plans.published", async (context, cancellationToken) => ((await _publish.ExecuteAsync(
            context, new PublishPlatformPlanVersionRequest { PlatformPlanId = platformPlanId }, cancellationToken)).Error, null));
    }

    public Task<bool> RejectAsync(Guid platformPlanId, string? reason)
    {
        return ActAsync("ui.plans.version_rejected", async (context, cancellationToken) => ((await _reject.ExecuteAsync(
            context, new RejectPlatformPlanVersionRequest { PlatformPlanId = platformPlanId, Reason = reason ?? string.Empty }, cancellationToken)).Error, null));
    }
}
