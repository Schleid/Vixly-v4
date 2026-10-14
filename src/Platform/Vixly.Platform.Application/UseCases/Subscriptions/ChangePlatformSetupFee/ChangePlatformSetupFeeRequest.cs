namespace Vixly.Platform.Application.UseCases.Subscriptions.ChangePlatformSetupFee;

// =========================================================
// FILE: ChangePlatformSetupFeeRequest.cs
//
// PURPOSE:
// Changes or waives the setup fee for one tenant, before it is charged (D-S23).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangePlatformSetupFeeRequest
{
    public Guid PlatformTenantId { get; init; }

    public decimal? Amount { get; init; }

    public string Reason { get; init; } = string.Empty;
}
