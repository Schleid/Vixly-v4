using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.CreateOperationsCustomer;

// =========================================================
// FILE: CreateOperationsCustomerResponse.cs
//
// PURPOSE:
// Lets staff enter a customer when the company's plan has no customer portal (Vixly Lite, owner 2026-10-30).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreateOperationsCustomerResponse
{
    private CreateOperationsCustomerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid CustomerId { get; private init; }

    public static CreateOperationsCustomerResponse Success(Guid customerId)
    {
        return new CreateOperationsCustomerResponse { Succeeded = true, CustomerId = customerId };
    }

    public static CreateOperationsCustomerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreateOperationsCustomerResponse { Error = error };
    }
}
