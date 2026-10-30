using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.HandleOperationsCardWebhook;

// =========================================================
// FILE: HandleOperationsCardWebhookUseCase.cs
//
// PURPOSE:
// Takes an event from the company's Stripe account and records a card payment it confirms (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - No signed-in person: the tenant comes from the host, the trust from the signature (5 minutes’ tolerance)
// - Unknown events and other charges are answered OK so Stripe stops sending them
// =========================================================
public sealed class HandleOperationsCardWebhookUseCase
{
    private readonly OperationsCardDesk _cards;

    private readonly IOperationsCardRepository _store;

    public HandleOperationsCardWebhookUseCase(
        OperationsCardDesk cards,
        IOperationsCardRepository store)
    {
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<HandleOperationsCardWebhookResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        HandleOperationsCardWebhookRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // Anonymous, from Stripe: trusted only when signed with the company's own webhook secret.
        if (context.TenantId is null || await _cards.WebhookSecretAsync(cancellationToken) is not { } webhookSecret
            || _cards.Gateway.ReadEvent(request.Payload, request.Signature, webhookSecret, _cards.UtcNow) is not { } received)
        {
            return HandleOperationsCardWebhookResponse.Failure(OperationsCardErrors.ProviderRefused);
        }

        if (received.ChargeId is not { } chargeId || await _store.GetChargeAsync(chargeId, cancellationToken) is not { } charge)
        {
            return HandleOperationsCardWebhookResponse.Success();
        }

        // Not saved (a race, or the invoice changed meanwhile): refused, so Stripe sends it again (review).
        if (received.State.Paid && !await _cards.RecordPaidAsync(charge, received.State, cancellationToken))
        {
            return HandleOperationsCardWebhookResponse.Failure(OperationsCardErrors.ProviderUnavailable);
        }
        else if (received.State.Over && await _cards.SecretKeyAsync(cancellationToken) is { } secret)
        {
            // Expired or cancelled at Stripe: asked again before cancelling here (never cancels a paid one).
            await _cards.RefreshAsync(secret, charge, cancellationToken);
        }

        return HandleOperationsCardWebhookResponse.Success();
    }
}
