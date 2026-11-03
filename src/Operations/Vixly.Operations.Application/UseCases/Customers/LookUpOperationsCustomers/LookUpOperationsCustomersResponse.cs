using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.LookUpOperationsCustomers;

// =========================================================
// FILE: LookUpOperationsCustomersResponse.cs
//
// PURPOSE:
// The customers that match exactly, or why none can be looked up.
//
// RESPONSIBILITIES:
// - Carry the matches, or the refusal
//
// IMPORTANT NOTES:
// - A few matches at most: a phone number shared by a family
// =========================================================
public sealed class LookUpOperationsCustomersResponse
{
    private LookUpOperationsCustomersResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsCustomerProfile> Customers { get; private init; } = Array.Empty<OperationsCustomerProfile>();

    public static LookUpOperationsCustomersResponse Success(IReadOnlyList<OperationsCustomerProfile> customers)
    {
        ArgumentNullException.ThrowIfNull(customers);

        return new LookUpOperationsCustomersResponse { Succeeded = true, Customers = customers };
    }

    public static LookUpOperationsCustomersResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new LookUpOperationsCustomersResponse { Error = error };
    }
}
