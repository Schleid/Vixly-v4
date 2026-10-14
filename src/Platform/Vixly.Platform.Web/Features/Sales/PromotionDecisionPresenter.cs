using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Sales.ApprovePlatformPromotion;
using Vixly.Platform.Application.UseCases.Sales.EndPlatformPromotion;
using Vixly.Platform.Application.UseCases.Sales.PausePlatformPromotion;
using Vixly.Platform.Application.UseCases.Sales.RejectPlatformPromotion;
using Vixly.Platform.Application.UseCases.Sales.ResumePlatformPromotion;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: PromotionDecisionPresenter.cs
//
// PURPOSE:
// Approves, rejects, pauses, resumes and ends a promotion (D-S26).
//
// RESPONSIBILITIES:
// - Run the use cases for its component and hold the state
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// =========================================================
public sealed class PromotionDecisionPresenter : SalesActionPresenter
{
    private readonly ApprovePlatformPromotionUseCase _approve;

    private readonly RejectPlatformPromotionUseCase _reject;

    private readonly PausePlatformPromotionUseCase _pause;

    private readonly ResumePlatformPromotionUseCase _resume;

    private readonly EndPlatformPromotionUseCase _end;

    public PromotionDecisionPresenter(
        ApprovePlatformPromotionUseCase approve,
        RejectPlatformPromotionUseCase reject,
        PausePlatformPromotionUseCase pause,
        ResumePlatformPromotionUseCase resume,
        EndPlatformPromotionUseCase end,
        PlatformUseCaseScope scope,
        ILogger<PromotionDecisionPresenter> logger)
        : base(scope, logger)
    {
        _approve = approve ?? throw new ArgumentNullException(nameof(approve));
        _reject = reject ?? throw new ArgumentNullException(nameof(reject));
        _pause = pause ?? throw new ArgumentNullException(nameof(pause));
        _resume = resume ?? throw new ArgumentNullException(nameof(resume));
        _end = end ?? throw new ArgumentNullException(nameof(end));
    }

    public Task<bool> ApproveAsync(Guid promotionId, string? reason)
    {
        return ActAsync("ui.promotions.approved", async (context, cancellationToken) => ((await _approve.ExecuteAsync(
            context, new ApprovePlatformPromotionRequest { PromotionId = promotionId, Reason = reason }, cancellationToken)).Error, null));
    }

    public Task<bool> RejectAsync(Guid promotionId, string? reason)
    {
        return ActAsync("ui.promotions.rejected", async (context, cancellationToken) => ((await _reject.ExecuteAsync(
            context, new RejectPlatformPromotionRequest { PromotionId = promotionId, Reason = reason }, cancellationToken)).Error, null));
    }

    public Task<bool> PauseAsync(Guid promotionId)
    {
        return ActAsync("ui.promotions.paused", async (context, cancellationToken) => ((await _pause.ExecuteAsync(
            context, new PausePlatformPromotionRequest { PromotionId = promotionId }, cancellationToken)).Error, null));
    }

    public Task<bool> ResumeAsync(Guid promotionId)
    {
        return ActAsync("ui.promotions.resumed", async (context, cancellationToken) => ((await _resume.ExecuteAsync(
            context, new ResumePlatformPromotionRequest { PromotionId = promotionId }, cancellationToken)).Error, null));
    }

    public Task<bool> EndAsync(Guid promotionId)
    {
        return ActAsync("ui.promotions.ended", async (context, cancellationToken) => ((await _end.ExecuteAsync(
            context, new EndPlatformPromotionRequest { PromotionId = promotionId }, cancellationToken)).Error, null));
    }
}
