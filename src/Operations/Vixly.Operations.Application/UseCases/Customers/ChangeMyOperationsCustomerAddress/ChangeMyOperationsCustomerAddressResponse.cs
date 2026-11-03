using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsCustomerAddress;

// =========================================================
// FILE: ChangeMyOperationsCustomerAddressResponse.cs
//
// PURPOSE:
// The signed-in customer gives or changes their home address (owner, 2026-10-26).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeMyOperationsCustomerAddressResponse
{
    private ChangeMyOperationsCustomerAddressResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsCustomerProfile? Customer { get; private init; }

    public static ChangeMyOperationsCustomerAddressResponse Success(OperationsCustomerProfile? customer)
    {
        return new ChangeMyOperationsCustomerAddressResponse { Succeeded = true, Customer = customer };
    }

    public static ChangeMyOperationsCustomerAddressResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeMyOperationsCustomerAddressResponse { Error = error };
    }
}
