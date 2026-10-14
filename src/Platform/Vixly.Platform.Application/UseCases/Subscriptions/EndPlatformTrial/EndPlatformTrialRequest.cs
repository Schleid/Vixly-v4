namespace Vixly.Platform.Application.UseCases.Subscriptions.EndPlatformTrial;

// =========================================================
// FILE: EndPlatformTrialRequest.cs
//
// PURPOSE:
// Ends a trial now: the subscription then awaits payment (owner, D-S12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class EndPlatformTrialRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
