using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.ListOperationsCustomers;

// =========================================================
// FILE: ListOperationsCustomersResponse.cs
//
// PURPOSE:
// Staff list the customers: search by name, email, phone or code, 25 per page (Slice 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsCustomersResponse
{
    private ListOperationsCustomersResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsCustomerProfile> Customers { get; private init; } = Array.Empty<OperationsCustomerProfile>();

    public int TotalCount { get; private init; }

    public static ListOperationsCustomersResponse Success(IReadOnlyList<OperationsCustomerProfile> customers, int totalCount)
    {
        return new ListOperationsCustomersResponse { Succeeded = true, Customers = customers, TotalCount = totalCount };
    }

    public static ListOperationsCustomersResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsCustomersResponse { Error = error };
    }
}
