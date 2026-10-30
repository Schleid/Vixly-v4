using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsCustomerPassword;

// =========================================================
// FILE: ChangeMyOperationsCustomerPasswordResponse.cs
//
// PURPOSE:
// The signed-in customer changes their password; their other sessions end (Slice 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeMyOperationsCustomerPasswordResponse
{
    private ChangeMyOperationsCustomerPasswordResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeMyOperationsCustomerPasswordResponse Success()
    {
        return new ChangeMyOperationsCustomerPasswordResponse { Succeeded = true };
    }

    public static ChangeMyOperationsCustomerPasswordResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeMyOperationsCustomerPasswordResponse { Error = error };
    }
}
