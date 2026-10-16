namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformTenantCards;

// =========================================================
// FILE: GetPlatformTenantCardsRequest.cs
//
// PURPOSE:
// Reads a tenant's saved cards and automatic-charging consent (D-P3, D-P4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformTenantCardsRequest
{
    public Guid PlatformTenantId { get; init; }
}
