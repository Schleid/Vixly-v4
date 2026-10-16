using Vixly.Platform.Application.Payments;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformProviderEventReceived.cs
//
// PURPOSE:
// A verified provider event waiting to be applied (D-P5).
//
// RESPONSIBILITIES:
// - Point to the stored event and carry its provider-neutral content
//
// IMPORTANT NOTES:
// - Handled by PlatformProviderEventHandler; retried by the outbox on failure
// =========================================================
public sealed record PlatformProviderEventReceived(Guid ProviderEventRecordId, PlatformCardEvent Event);
