namespace Vixly.Platform.Application.UseCases.Payments.OpenPlatformPaymentLink;

// =========================================================
// FILE: OpenPlatformPaymentLinkRequest.cs
//
// PURPOSE:
// Shows what a pay or save-card link is for, before the tenant continues to the provider.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class OpenPlatformPaymentLinkRequest
{
    public string? Token { get; init; }
}
