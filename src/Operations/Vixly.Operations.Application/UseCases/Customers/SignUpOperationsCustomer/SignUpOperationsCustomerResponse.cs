using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.SignUpOperationsCustomer;

// =========================================================
// FILE: SignUpOperationsCustomerResponse.cs
//
// PURPOSE:
// A customer signs up on the tenant portal (Slice 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SignUpOperationsCustomerResponse
{
    private SignUpOperationsCustomerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SignUpOperationsCustomerResponse Success()
    {
        return new SignUpOperationsCustomerResponse { Succeeded = true };
    }

    public static SignUpOperationsCustomerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SignUpOperationsCustomerResponse { Error = error };
    }
}
