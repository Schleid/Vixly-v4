using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Tenants.CorrectPlatformTenantSlug;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantSlugPresenter.cs
//
// PURPOSE:
// Behaviour of the portal address correction (D-T11).
//
// RESPONSIBILITIES:
// - Correct the slug with a reason, after step-up
//
// IMPORTANT NOTES:
// - Offered only while the tenant has never been active
// =========================================================
public sealed class TenantSlugPresenter : PlatformPresenter<TenantSlugState>
{
    private readonly CorrectPlatformTenantSlugUseCase _correct;

    public TenantSlugPresenter(CorrectPlatformTenantSlugUseCase correct, PlatformUseCaseScope scope, ILogger<TenantSlugPresenter> logger)
        : base(scope, TenantSlugState.Initial, logger)
    {
        _correct = correct ?? throw new ArgumentNullException(nameof(correct));
    }

    public void StartEdit()
    {
        SetState(new TenantSlugState(true, false, null, null));
    }

    public void Cancel()
    {
        SetState(TenantSlugState.Initial);
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public async Task<bool> CorrectAsync(Guid platformTenantId, string? slug, string? reason)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _correct.ExecuteAsync(
            context,
            new CorrectPlatformTenantSlugRequest { PlatformTenantId = platformTenantId, Slug = slug ?? string.Empty, Reason = reason ?? string.Empty },
            cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return false;
        }

        SetState(error is null ? new TenantSlugState(false, false, "ui.tenants.slug_corrected", null) : State with { StepUpPending = false, Error = error });

        return error is null;
    }
}
