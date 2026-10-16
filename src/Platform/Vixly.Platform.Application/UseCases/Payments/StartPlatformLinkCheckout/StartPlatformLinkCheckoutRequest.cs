namespace Vixly.Platform.Application.UseCases.Payments.StartPlatformLinkCheckout;

// =========================================================
// FILE: StartPlatformLinkCheckoutRequest.cs
//
// PURPOSE:
// Opens the provider's hosted page from a pay or save-card link (D-P2, D-P7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StartPlatformLinkCheckoutRequest
{
    public string? Token { get; init; }

    public bool SaveCard { get; init; }

    public bool AutoCharge { get; init; }
}
