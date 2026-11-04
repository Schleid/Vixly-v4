using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.ResetOperationsCustomerPassword;

// =========================================================
// FILE: ResetOperationsCustomerPasswordResponse.cs
//
// PURPOSE:
// Sets a new password from a reset or invitation link, ends every session and signs in (Slice 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResetOperationsCustomerPasswordResponse
{
    private ResetOperationsCustomerPasswordResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string SessionToken { get; private init; } = string.Empty;

    public OperationsCustomerProfile? Customer { get; private init; }

    public static ResetOperationsCustomerPasswordResponse Success(string sessionToken, OperationsCustomerProfile? customer)
    {
        return new ResetOperationsCustomerPasswordResponse { Succeeded = true, SessionToken = sessionToken, Customer = customer };
    }

    public static ResetOperationsCustomerPasswordResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResetOperationsCustomerPasswordResponse { Error = error };
    }
}
