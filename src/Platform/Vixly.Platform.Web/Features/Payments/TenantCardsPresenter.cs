using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments.GetPlatformTenantCards;
using Vixly.Platform.Application.UseCases.Payments.RemovePlatformSavedCard;
using Vixly.Platform.Application.UseCases.Payments.SendPlatformSaveCardLink;
using Vixly.Platform.Application.UseCases.Payments.TurnOffPlatformAutoCharge;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: TenantCardsPresenter.cs
//
// PURPOSE:
// Behaviour of the Saved cards part of the tenant page (D-P3, D-P4).
//
// RESPONSIBILITIES:
// - Load cards and automatic-charging consent
// - Send a save-card link, remove a card, turn automatic charging off
//
// IMPORTANT NOTES:
// - Staff never type card details and never turn automatic charging on
// =========================================================
public sealed class TenantCardsPresenter : PlatformPresenter<TenantCardsState>
{
    private readonly GetPlatformTenantCardsUseCase _get;

    private readonly SendPlatformSaveCardLinkUseCase _link;

    private readonly RemovePlatformSavedCardUseCase _remove;

    private readonly TurnOffPlatformAutoChargeUseCase _off;

    public TenantCardsPresenter(
        GetPlatformTenantCardsUseCase get,
        SendPlatformSaveCardLinkUseCase link,
        RemovePlatformSavedCardUseCase remove,
        TurnOffPlatformAutoChargeUseCase off,
        PlatformUseCaseScope scope,
        ILogger<TenantCardsPresenter> logger)
        : base(scope, TenantCardsState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _link = link ?? throw new ArgumentNullException(nameof(link));
        _remove = remove ?? throw new ArgumentNullException(nameof(remove));
        _off = off ?? throw new ArgumentNullException(nameof(off));
    }

    public async Task LoadAsync(Guid tenantId)
    {
        GetPlatformTenantCardsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformTenantCardsRequest { PlatformTenantId = tenantId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Cards = response.Cards, Loaded = true }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public async Task SendLinkAsync(Guid tenantId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _link.ExecuteAsync(context, new SendPlatformSaveCardLinkRequest { PlatformTenantId = tenantId }, cancellationToken)).Error);
        await FinishAsync(tenantId, error, "ui.saved_cards.link_sent");
    }

    public async Task RemoveAsync(Guid tenantId, Guid cardId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _remove.ExecuteAsync(context, new RemovePlatformSavedCardRequest { PlatformTenantId = tenantId, SavedCardId = cardId }, cancellationToken)).Error);
        await FinishAsync(tenantId, error, "ui.saved_cards.removed");
    }

    public async Task TurnOffAsync(Guid tenantId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _off.ExecuteAsync(context, new TurnOffPlatformAutoChargeRequest { PlatformTenantId = tenantId }, cancellationToken)).Error);
        await FinishAsync(tenantId, error, "ui.saved_cards.auto_charge_turned_off");
    }

    private async Task FinishAsync(Guid tenantId, Error? error, string doneKey)
    {
        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return;
        }

        await LoadAsync(tenantId);
        SetState(State with { StepUpPending = false, DoneKey = error is null ? doneKey : null, Error = error });
    }
}
