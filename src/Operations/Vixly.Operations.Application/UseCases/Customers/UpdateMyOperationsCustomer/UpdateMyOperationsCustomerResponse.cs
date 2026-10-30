using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.UpdateMyOperationsCustomer;

// =========================================================
// FILE: UpdateMyOperationsCustomerResponse.cs
//
// PURPOSE:
// The signed-in customer changes their name, company, phone or language (Slice 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdateMyOperationsCustomerResponse
{
    private UpdateMyOperationsCustomerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsCustomerProfile? Customer { get; private init; }

    public static UpdateMyOperationsCustomerResponse Success(OperationsCustomerProfile? customer)
    {
        return new UpdateMyOperationsCustomerResponse { Succeeded = true, Customer = customer };
    }

    public static UpdateMyOperationsCustomerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdateMyOperationsCustomerResponse { Error = error };
    }
}
