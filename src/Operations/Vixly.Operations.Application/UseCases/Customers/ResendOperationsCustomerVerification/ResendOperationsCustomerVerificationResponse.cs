using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.ResendOperationsCustomerVerification;

// =========================================================
// FILE: ResendOperationsCustomerVerificationResponse.cs
//
// PURPOSE:
// Sends a new confirmation link; the same answer for unknown emails (Slice 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResendOperationsCustomerVerificationResponse
{
    private ResendOperationsCustomerVerificationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResendOperationsCustomerVerificationResponse Success()
    {
        return new ResendOperationsCustomerVerificationResponse { Succeeded = true };
    }

    public static ResendOperationsCustomerVerificationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResendOperationsCustomerVerificationResponse { Error = error };
    }
}
