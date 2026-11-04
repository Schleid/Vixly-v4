using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.ChangeOperationsCustomerStatus;

// =========================================================
// FILE: ChangeOperationsCustomerStatusResponse.cs
//
// PURPOSE:
// Staff suspend, reactivate or close a customer with a reason; the customer's sessions end (Slice 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsCustomerStatusResponse
{
    private ChangeOperationsCustomerStatusResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsCustomerStatusResponse Success()
    {
        return new ChangeOperationsCustomerStatusResponse { Succeeded = true };
    }

    public static ChangeOperationsCustomerStatusResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsCustomerStatusResponse { Error = error };
    }
}
