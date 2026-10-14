namespace Vixly.Platform.Application.UseCases.Subscriptions.CancelPlatformOneTimeCharge;

// =========================================================
// FILE: CancelPlatformOneTimeChargeRequest.cs
//
// PURPOSE:
// Cancels a one-time charge that is not billed yet (D-S23).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CancelPlatformOneTimeChargeRequest
{
    public Guid PlatformTenantId { get; init; }

    public Guid ChargeId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
