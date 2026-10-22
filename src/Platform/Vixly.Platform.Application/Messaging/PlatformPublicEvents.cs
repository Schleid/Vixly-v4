using Vixly.Contracts.Platform.Events;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformPublicEvents.cs
//
// PURPOSE:
// Which internal outbox messages become public events, and their wire payloads (Slice 9, D-K3).
//
// RESPONSIBILITIES:
// - List the outbox types that publish
// - Map one outbox message to its public events
//
// IMPORTANT NOTES:
// - Only these types leave Platform; every other message stays internal
// - Payloads are Contracts.Platform records with string codes (D-K10)
// =========================================================
public static class PlatformPublicEvents
{
    public static IReadOnlyList<string> SourceTypes { get; } = new[]
    {
        PlatformMessageTypes.TenantStatusChanged,
        PlatformMessageTypes.TenantSlugCorrected,
        PlatformMessageTypes.SubscriptionStarted,
        PlatformMessageTypes.SubscriptionChanged,
        PlatformMessageTypes.SubscriptionRenewed,
        PlatformMessageTypes.SubscriptionStatusChanged,
        PlatformMessageTypes.SubscriptionEnded,
        PlatformMessageTypes.PlanVersionPublished,
        PlatformMessageTypes.DeviceApproved,
        PlatformMessageTypes.DeviceReleased,
        PlatformMessageTypes.DeviceRevoked,
        PlatformMessageTypes.LegalVersionPublished,
        PlatformMessageTypes.TermsAccepted,
        PlatformMessageTypes.RegistrationAccountSetupReady
    };

    public static IReadOnlyList<PlatformPublicEvent> From(PlatformOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        switch (message.Type)
        {
            case PlatformMessageTypes.TenantStatusChanged:
            {
                PlatformTenantStatusChanged item = PlatformOutboxWriter.Read<PlatformTenantStatusChanged>(message);
                return One(PlatformEventTypes.TenantStatusChanged, item.PlatformTenantId, item.OccurredUtc, new PlatformTenantStatusChangedEvent(
                    item.PlatformTenantId, item.TenantCode, PlatformWireCodes.Of(item.FromStatus), PlatformWireCodes.Of(item.ToStatus), PlatformWireCodes.Of(item.Category), item.OccurredUtc));
            }

            case PlatformMessageTypes.TenantSlugCorrected:
            {
                PlatformTenantSlugCorrected item = PlatformOutboxWriter.Read<PlatformTenantSlugCorrected>(message);
                return One(PlatformEventTypes.TenantSlugCorrected, item.PlatformTenantId, item.OccurredUtc,
                    new PlatformTenantSlugCorrectedEvent(item.PlatformTenantId, item.FromSlug, item.ToSlug, item.OccurredUtc));
            }

            case PlatformMessageTypes.SubscriptionStarted:
            case PlatformMessageTypes.SubscriptionChanged:
            case PlatformMessageTypes.SubscriptionRenewed:
            case PlatformMessageTypes.SubscriptionStatusChanged:
            case PlatformMessageTypes.SubscriptionEnded:
            {
                PlatformSubscriptionMessage item = PlatformOutboxWriter.Read<PlatformSubscriptionMessage>(message);
                List<PlatformPublicEvent> events = new()
                {
                    new(PlatformEventTypes.SubscriptionStatusChanged, item.PlatformTenantId, item.OccurredUtc, new PlatformSubscriptionStatusChangedEvent(
                        item.PlatformTenantId, item.PlatformSubscriptionId, PlatformWireCodes.Of(item.Status), item.EventCode, item.PlatformPlanId,
                        PlatformWireCodes.Of(item.Term), item.Licences, item.PeriodEndUtc, item.OccurredUtc))
                };

                if (message.Type is PlatformMessageTypes.SubscriptionStarted or PlatformMessageTypes.SubscriptionChanged or PlatformMessageTypes.SubscriptionRenewed)
                {
                    events.Add(Seats(item.PlatformTenantId, "subscription", item.OccurredUtc));
                }

                return events;
            }

            case PlatformMessageTypes.PlanVersionPublished:
            {
                PlatformPlanVersionPublished item = PlatformOutboxWriter.Read<PlatformPlanVersionPublished>(message);
                return One(PlatformEventTypes.PlanVersionPublished, null, item.OccurredUtc,
                    new PlatformPlanVersionPublishedEvent(item.PlatformPlanId, item.PlanCode, item.VersionId, item.VersionNumber, item.OccurredUtc));
            }

            case PlatformMessageTypes.DeviceApproved:
            case PlatformMessageTypes.DeviceReleased:
            case PlatformMessageTypes.DeviceRevoked:
            {
                PlatformDeviceChanged item = PlatformOutboxWriter.Read<PlatformDeviceChanged>(message);
                string type = message.Type switch
                {
                    PlatformMessageTypes.DeviceApproved => PlatformEventTypes.DeviceApproved,
                    PlatformMessageTypes.DeviceReleased => PlatformEventTypes.DeviceReleased,
                    _ => PlatformEventTypes.DeviceRevoked
                };

                return new[]
                {
                    new PlatformPublicEvent(type, item.PlatformTenantId, item.OccurredUtc, new PlatformDeviceChangedEvent(
                        item.PlatformTenantId, item.DeviceId, item.Number, PlatformWireCodes.Of(item.Status), PlatformWireCodes.Of(item.Reason), item.OccurredUtc)),
                    Seats(item.PlatformTenantId, "device", item.OccurredUtc)
                };
            }

            case PlatformMessageTypes.LegalVersionPublished:
            {
                PlatformLegalVersionPublished item = PlatformOutboxWriter.Read<PlatformLegalVersionPublished>(message);
                return One(PlatformEventTypes.LegalVersionPublished, null, item.OccurredUtc, new PlatformLegalVersionPublishedEvent(
                    item.DocumentId, PlatformWireCodes.Of(item.Kind), item.Version, item.AcceptanceRequired, item.EffectiveUtc, item.DeadlineUtc, item.OccurredUtc));
            }

            case PlatformMessageTypes.TermsAccepted:
            {
                PlatformTermsAccepted item = PlatformOutboxWriter.Read<PlatformTermsAccepted>(message);
                return One(PlatformEventTypes.TermsAccepted, item.PlatformTenantId, item.OccurredUtc,
                    new PlatformTermsAcceptedEvent(item.PlatformTenantId, item.DocumentId, item.Version, PlatformWireCodes.Of(item.Source), item.OccurredUtc));
            }

            case PlatformMessageTypes.RegistrationAccountSetupReady:
            {
                PlatformRegistrationAccountSetupReady item = PlatformOutboxWriter.Read<PlatformRegistrationAccountSetupReady>(message);
                return One(PlatformEventTypes.RegistrationAccountSetupReady, item.PlatformTenantId, item.OccurredUtc,
                    new PlatformRegistrationAccountSetupReadyEvent(item.PlatformTenantId, item.RegistrationId, item.OccurredUtc));
            }
            default:
                return Array.Empty<PlatformPublicEvent>();
        }
    }

    private static PlatformPublicEvent[] One(string type, Guid? tenantId, DateTimeOffset occurredUtc, object data)
    {
        return new[] { new PlatformPublicEvent(type, tenantId, occurredUtc, data) };
    }

    private static PlatformPublicEvent Seats(Guid tenantId, string cause, DateTimeOffset occurredUtc)
    {
        return new PlatformPublicEvent(PlatformEventTypes.SeatsChanged, tenantId, occurredUtc, new PlatformSeatsChangedEvent(tenantId, cause, occurredUtc));
    }
}
