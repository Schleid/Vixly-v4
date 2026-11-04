using Vixly.Operations.Application.Counter;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.FindOperationsCounterCustomers;

// =========================================================
// FILE: FindOperationsCounterCustomersResponse.cs
//
// PURPOSE:
// Finds a customer at the front-desk counter by phone, email or customer code (Slice 5C, C-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class FindOperationsCounterCustomersResponse
{
    private FindOperationsCounterCustomersResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsCounterCustomer> Customers { get; private init; } = Array.Empty<OperationsCounterCustomer>();

    public static FindOperationsCounterCustomersResponse Success(IReadOnlyList<OperationsCounterCustomer> customers)
    {
        return new FindOperationsCounterCustomersResponse { Succeeded = true, Customers = customers };
    }

    public static FindOperationsCounterCustomersResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new FindOperationsCounterCustomersResponse { Error = error };
    }
}
