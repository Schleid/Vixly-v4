using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.DisconnectOperationsCardProvider;

// =========================================================
// FILE: DisconnectOperationsCardProviderResponse.cs
//
// PURPOSE:
// Disconnects the company's Stripe account; cards are no longer offered (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DisconnectOperationsCardProviderResponse
{
    private DisconnectOperationsCardProviderResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DisconnectOperationsCardProviderResponse Success()
    {
        return new DisconnectOperationsCardProviderResponse { Succeeded = true };
    }

    public static DisconnectOperationsCardProviderResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DisconnectOperationsCardProviderResponse { Error = error };
    }
}
