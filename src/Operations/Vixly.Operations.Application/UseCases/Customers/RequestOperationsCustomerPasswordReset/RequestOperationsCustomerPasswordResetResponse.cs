using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.RequestOperationsCustomerPasswordReset;

// =========================================================
// FILE: RequestOperationsCustomerPasswordResetResponse.cs
//
// PURPOSE:
// Emails a password-reset link; the same answer for unknown emails (Slice 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RequestOperationsCustomerPasswordResetResponse
{
    private RequestOperationsCustomerPasswordResetResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RequestOperationsCustomerPasswordResetResponse Success()
    {
        return new RequestOperationsCustomerPasswordResetResponse { Succeeded = true };
    }

    public static RequestOperationsCustomerPasswordResetResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RequestOperationsCustomerPasswordResetResponse { Error = error };
    }
}
