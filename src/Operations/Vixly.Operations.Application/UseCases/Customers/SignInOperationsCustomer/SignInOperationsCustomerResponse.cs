using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.SignInOperationsCustomer;

// =========================================================
// FILE: SignInOperationsCustomerResponse.cs
//
// PURPOSE:
// A customer signs in with email and password (Slice 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SignInOperationsCustomerResponse
{
    private SignInOperationsCustomerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public OperationsCustomerProfile? Customer { get; private init; }

    public static SignInOperationsCustomerResponse Success(string sessionToken, OperationsCustomerProfile? customer)
    {
        return new SignInOperationsCustomerResponse { Succeeded = true, SessionToken = sessionToken, Customer = customer };
    }

    public static SignInOperationsCustomerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SignInOperationsCustomerResponse { Error = error };
    }
}
