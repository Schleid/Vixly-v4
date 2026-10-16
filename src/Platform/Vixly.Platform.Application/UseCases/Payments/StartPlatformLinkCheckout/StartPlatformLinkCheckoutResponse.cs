using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.StartPlatformLinkCheckout;

// =========================================================
// FILE: StartPlatformLinkCheckoutResponse.cs
//
// PURPOSE:
// Opens the provider's hosted page from a pay or save-card link (D-P2, D-P7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartPlatformLinkCheckoutResponse
{
    private StartPlatformLinkCheckoutResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string CheckoutUrl { get; private init; } = string.Empty;

    public static StartPlatformLinkCheckoutResponse Success(string checkoutUrl)
    {
        return new StartPlatformLinkCheckoutResponse { Succeeded = true, CheckoutUrl = checkoutUrl };
    }

    public static StartPlatformLinkCheckoutResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartPlatformLinkCheckoutResponse { Error = error };
    }
}
