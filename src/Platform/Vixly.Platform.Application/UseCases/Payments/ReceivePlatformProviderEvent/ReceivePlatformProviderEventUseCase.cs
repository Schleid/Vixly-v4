using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.ReceivePlatformProviderEvent;

// =========================================================
// FILE: ReceivePlatformProviderEventUseCase.cs
//
// PURPOSE:
// Accepts a webhook from the card provider: checks its signature, keeps it once and queues it (D-P5).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous; only a valid signature is accepted (S-6)
// - Applied later by PlatformProviderEventHandler through the outbox
// =========================================================
public sealed class ReceivePlatformProviderEventUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly PlatformOutboxWriter _outbox;

    public ReceivePlatformProviderEventUseCase(
        PlatformPaymentsAdministration payments,
        PlatformOutboxWriter outbox)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<ReceivePlatformProviderEventResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        ReceivePlatformProviderEventRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformCardEvent card;

        try
        {
            card = _payments.Gateway.ParseEvent(request.Payload ?? string.Empty, request.Signature);
        }
        catch (DomainException exception)
        {
            return ReceivePlatformProviderEventResponse.Failure(exception.Error);
        }

        if (card.Kind == PlatformCardEventKind.Ignored)
        {
            return ReceivePlatformProviderEventResponse.Success(false);
        }

        PlatformCardProvider provider = _payments.Gateway.Status.Provider;

        if (await _payments.Store.EventSeenAsync(provider, card.EventId, cancellationToken))
        {
            return ReceivePlatformProviderEventResponse.Success(true);
        }

        PlatformProviderEvent record = PlatformProviderEvent.Receive(provider, card.EventId, card.EventType, card.ObjectId, _payments.UtcNow);
        _payments.Store.AddEvent(record);
        // Events about one tenant stay in that tenant's order; the rest share one provider stream.
        Guid stream = PlatformCardMetadata.Id(card.Metadata, PlatformCardMetadata.Tenant) ?? PlatformCardMetadata.ProviderStream;
        _outbox.Write(PlatformMessageTypes.ProviderEvent, stream, new PlatformProviderEventReceived(record.Id, card));

        // A second delivery racing this one hits the unique event id: it is the same event.
        return ReceivePlatformProviderEventResponse.Success(!await _payments.SaveAsync(cancellationToken));
    }
}
