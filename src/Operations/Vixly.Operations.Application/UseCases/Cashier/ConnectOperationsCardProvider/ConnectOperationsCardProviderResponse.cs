using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.ConnectOperationsCardProvider;

// =========================================================
// FILE: ConnectOperationsCardProviderResponse.cs
//
// PURPOSE:
// Connects the company's own Stripe account with its keys (Slice 5F step 4; owner 2026-10-21).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ConnectOperationsCardProviderResponse
{
    private ConnectOperationsCardProviderResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ConnectOperationsCardProviderResponse Success()
    {
        return new ConnectOperationsCardProviderResponse { Succeeded = true };
    }

    public static ConnectOperationsCardProviderResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ConnectOperationsCardProviderResponse { Error = error };
    }
}
