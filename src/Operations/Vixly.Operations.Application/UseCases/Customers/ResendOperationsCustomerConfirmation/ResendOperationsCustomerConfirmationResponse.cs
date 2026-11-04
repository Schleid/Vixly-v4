using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.ResendOperationsCustomerConfirmation;

// =========================================================
// FILE: ResendOperationsCustomerConfirmationResponse.cs
//
// PURPOSE:
// Result of ResendOperationsCustomerConfirmation.
//
// RESPONSIBILITIES:
// - Report success, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResendOperationsCustomerConfirmationResponse
{
    private ResendOperationsCustomerConfirmationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResendOperationsCustomerConfirmationResponse Success()
    {
        return new ResendOperationsCustomerConfirmationResponse { Succeeded = true };
    }

    public static ResendOperationsCustomerConfirmationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResendOperationsCustomerConfirmationResponse { Error = error };
    }
}
