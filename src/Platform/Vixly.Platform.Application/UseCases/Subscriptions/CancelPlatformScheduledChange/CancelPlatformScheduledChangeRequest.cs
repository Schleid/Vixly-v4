namespace Vixly.Platform.Application.UseCases.Subscriptions.CancelPlatformScheduledChange;

// =========================================================
// FILE: CancelPlatformScheduledChangeRequest.cs
//
// PURPOSE:
// Cancels the change waiting for the period end.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CancelPlatformScheduledChangeRequest
{
    public Guid PlatformTenantId { get; init; }
}
