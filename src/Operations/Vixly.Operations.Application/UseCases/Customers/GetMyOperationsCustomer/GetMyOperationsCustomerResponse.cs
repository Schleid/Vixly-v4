using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.GetMyOperationsCustomer;

// =========================================================
// FILE: GetMyOperationsCustomerResponse.cs
//
// PURPOSE:
// The signed-in customer's profile and mailbox addresses (Slice 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsCustomerResponse
{
    private GetMyOperationsCustomerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsCustomerProfile? Customer { get; private init; }

    public static GetMyOperationsCustomerResponse Success(OperationsCustomerProfile? customer)
    {
        return new GetMyOperationsCustomerResponse { Succeeded = true, Customer = customer };
    }

    public static GetMyOperationsCustomerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsCustomerResponse { Error = error };
    }
}
