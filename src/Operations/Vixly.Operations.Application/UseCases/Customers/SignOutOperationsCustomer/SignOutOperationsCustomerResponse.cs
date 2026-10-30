using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.SignOutOperationsCustomer;

// =========================================================
// FILE: SignOutOperationsCustomerResponse.cs
//
// PURPOSE:
// Ends the customer's current session (Slice 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SignOutOperationsCustomerResponse
{
    private SignOutOperationsCustomerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SignOutOperationsCustomerResponse Success()
    {
        return new SignOutOperationsCustomerResponse { Succeeded = true };
    }

    public static SignOutOperationsCustomerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SignOutOperationsCustomerResponse { Error = error };
    }
}
