using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.StartRegistrationCheckout;

// =========================================================
// FILE: StartRegistrationCheckoutResponse.cs
//
// PURPOSE:
// Opens the provider's hosted page for the registration invoice; the amount comes from the server (D-P7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartRegistrationCheckoutResponse
{
    private StartRegistrationCheckoutResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string CheckoutUrl { get; private init; } = string.Empty;

    public static StartRegistrationCheckoutResponse Success(string checkoutUrl)
    {
        return new StartRegistrationCheckoutResponse { Succeeded = true, CheckoutUrl = checkoutUrl };
    }

    public static StartRegistrationCheckoutResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartRegistrationCheckoutResponse { Error = error };
    }
}
