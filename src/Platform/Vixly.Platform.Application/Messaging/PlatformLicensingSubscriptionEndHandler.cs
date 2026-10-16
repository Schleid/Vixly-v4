using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformLicensingSubscriptionEndHandler.cs
//
// PURPOSE:
// Revokes every device when the subscription ends (D-L9).
//
// RESPONSIBILITIES:
// - Revoke open devices when the subscription-ended message arrives
//
// IMPORTANT NOTES:
// - A new subscription later starts with no devices: the tenant enrols them again
// =========================================================
public sealed class PlatformLicensingSubscriptionEndHandler : IPlatformOutboxHandler
{
    private readonly PlatformLicensingAdministration _licensing;

    public PlatformLicensingSubscriptionEndHandler(PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public string MessageType => PlatformMessageTypes.SubscriptionEnded;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformSubscriptionMessage ended = PlatformOutboxWriter.Read<PlatformSubscriptionMessage>(message);
        await PlatformLicensingEnd.RevokeAllAsync(_licensing, ended.PlatformTenantId, PlatformDeviceReason.SubscriptionEnded, cancellationToken);
    }
}
