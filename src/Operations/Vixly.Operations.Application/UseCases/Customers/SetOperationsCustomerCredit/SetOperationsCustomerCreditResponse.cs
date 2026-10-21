using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.SetOperationsCustomerCredit;

// =========================================================
// FILE: SetOperationsCustomerCreditResponse.cs
//
// PURPOSE:
// Staff allow or remove credit for a customer, with a reason (Slice 2; used by the payment gate from Slice 9).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetOperationsCustomerCreditResponse
{
    private SetOperationsCustomerCreditResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetOperationsCustomerCreditResponse Success()
    {
        return new SetOperationsCustomerCreditResponse { Succeeded = true };
    }

    public static SetOperationsCustomerCreditResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetOperationsCustomerCreditResponse { Error = error };
    }
}
