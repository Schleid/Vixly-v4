using Vixly.Platform.Application.UseCases.Sales;

namespace Vixly.Platform.Application.UseCases.Subscriptions.GrantPlatformDiscount;

// =========================================================
// FILE: GrantPlatformDiscountRequest.cs
//
// PURPOSE:
// Grants a discount to one tenant, or asks the sales manager when it is beyond the limits (D-S18, D-S22, D-S24).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GrantPlatformDiscountRequest
{
    public Guid PlatformTenantId { get; init; }

    public PlatformDiscountTermsInput? Terms { get; init; }

    public string Reason { get; init; } = string.Empty;
}
