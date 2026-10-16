namespace Vixly.Platform.Application.UseCases.Payments.StartRegistrationCheckout;

// =========================================================
// FILE: StartRegistrationCheckoutRequest.cs
//
// PURPOSE:
// Opens the provider's hosted page for the registration invoice; the amount comes from the server (D-P7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StartRegistrationCheckoutRequest
{
    public string? AccessToken { get; init; }

    public bool SaveCard { get; init; }

    public bool AutoCharge { get; init; }
}
