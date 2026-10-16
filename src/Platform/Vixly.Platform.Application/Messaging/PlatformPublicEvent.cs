namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformPublicEvent.cs
//
// PURPOSE:
// One public event made from an outbox message.
//
// RESPONSIBILITIES:
// - Carry the type, tenant, time and wire payload
//
// IMPORTANT NOTES:
// - Data is a Contracts.Platform.Events record
// =========================================================
public sealed record PlatformPublicEvent(string Type, Guid? TenantId, DateTimeOffset OccurredUtc, object Data);
