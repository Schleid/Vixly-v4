namespace Vixly.Platform.Application.UseCases.Payments.TurnOffPlatformAutoCharge;

// =========================================================
// FILE: TurnOffPlatformAutoChargeRequest.cs
//
// PURPOSE:
// Stops automatic charging for a tenant; only the tenant can turn it back on (D-P4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class TurnOffPlatformAutoChargeRequest
{
    public Guid PlatformTenantId { get; init; }
}
