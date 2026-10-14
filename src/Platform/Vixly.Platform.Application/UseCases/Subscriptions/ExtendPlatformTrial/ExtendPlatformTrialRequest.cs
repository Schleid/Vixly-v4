namespace Vixly.Platform.Application.UseCases.Subscriptions.ExtendPlatformTrial;

// =========================================================
// FILE: ExtendPlatformTrialRequest.cs
//
// PURPOSE:
// Moves a trial's end date (owner, D-S12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ExtendPlatformTrialRequest
{
    public Guid PlatformTenantId { get; init; }

    public DateTimeOffset TrialEndsUtc { get; init; }

    public string Reason { get; init; } = string.Empty;
}
