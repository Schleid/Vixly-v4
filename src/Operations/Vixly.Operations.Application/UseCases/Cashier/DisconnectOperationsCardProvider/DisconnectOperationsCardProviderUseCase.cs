using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.DisconnectOperationsCardProvider;

// =========================================================
// FILE: DisconnectOperationsCardProviderUseCase.cs
//
// PURPOSE:
// Disconnects the company's Stripe account; cards are no longer offered (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.settings.edit and a fresh second factor
// - Payments already made stay as they are; refunds by card need the account connected
// =========================================================
public sealed class DisconnectOperationsCardProviderUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCardRepository _cards;

    private readonly Vixly.Operations.Application.Cards.OperationsCardDesk _desk;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public DisconnectOperationsCardProviderUseCase(
        OperationsAccess access,
        IOperationsCardRepository cards,
        Vixly.Operations.Application.Cards.OperationsCardDesk desk,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DisconnectOperationsCardProviderResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        DisconnectOperationsCardProviderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, true, cancellationToken);

        if (refused is not null)
        {
            return DisconnectOperationsCardProviderResponse.Failure(refused);
        }

        if (await _cards.GetProviderAsync(cancellationToken) is not { } provider)
        {
            return DisconnectOperationsCardProviderResponse.Success();
        }

        // Review: no payment is left open on an account the company no longer reads.
        if (!await _desk.CloseOpenAsync(await _desk.SecretKeyAsync(cancellationToken), cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return DisconnectOperationsCardProviderResponse.Failure(OperationsCardErrors.PaymentsOpen);
        }

        _cards.Remove(provider);

        if (await _settings.FindAsync(cancellationToken) is { } settings)
        {
            settings.ChangeCard(false, _timeProvider.GetUtcNow());
        }

        _audit.Record(access!.User.Id, "ops.payments.card_disconnected", nameof(OperationsCardProvider), provider.Id, before: new { key = "…" + provider.KeyHint, live = provider.Live });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? DisconnectOperationsCardProviderResponse.Success()
            : DisconnectOperationsCardProviderResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
