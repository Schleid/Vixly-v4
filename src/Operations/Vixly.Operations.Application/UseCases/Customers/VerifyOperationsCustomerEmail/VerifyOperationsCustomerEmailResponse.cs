using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.VerifyOperationsCustomerEmail;

// =========================================================
// FILE: VerifyOperationsCustomerEmailResponse.cs
//
// PURPOSE:
// Confirms a customer's email from the link: the account becomes active, gets its code and is signed in (Slice 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class VerifyOperationsCustomerEmailResponse
{
    private VerifyOperationsCustomerEmailResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public OperationsCustomerProfile? Customer { get; private init; }

    public static VerifyOperationsCustomerEmailResponse Success(string sessionToken, OperationsCustomerProfile? customer)
    {
        return new VerifyOperationsCustomerEmailResponse { Succeeded = true, SessionToken = sessionToken, Customer = customer };
    }

    public static VerifyOperationsCustomerEmailResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new VerifyOperationsCustomerEmailResponse { Error = error };
    }
}
