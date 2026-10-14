namespace Vixly.Platform.Application.UseCases.Subscriptions.EndPlatformDiscount;

// =========================================================
// FILE: EndPlatformDiscountRequest.cs
//
// PURPOSE:
// Ends the tenant's current discount now, with a reason.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class EndPlatformDiscountRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
