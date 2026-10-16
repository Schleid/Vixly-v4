namespace Vixly.Platform.Application.UseCases.Payments.SendPlatformSaveCardLink;

// =========================================================
// FILE: SendPlatformSaveCardLinkRequest.cs
//
// PURPOSE:
// Emails the tenant's billing contact a link to save a card on the provider's page (D-P3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SendPlatformSaveCardLinkRequest
{
    public Guid PlatformTenantId { get; init; }
}
