namespace Vixly.Platform.Application.UseCases.Subscriptions.AddPlatformOneTimeCharge;

// =========================================================
// FILE: AddPlatformOneTimeChargeRequest.cs
//
// PURPOSE:
// Adds a one-time charge, billed on the next invoice (D-S23).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AddPlatformOneTimeChargeRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Description { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Reason { get; init; } = string.Empty;
}
