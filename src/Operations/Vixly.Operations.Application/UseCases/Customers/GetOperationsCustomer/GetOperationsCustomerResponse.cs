using Vixly.Operations.Application.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.GetOperationsCustomer;

// =========================================================
// FILE: GetOperationsCustomerResponse.cs
//
// PURPOSE:
// Staff open one customer, with the packages coming to them (Slice 2; owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCustomerResponse
{
    private GetOperationsCustomerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsCustomerProfile? Customer { get; private init; }

    public IReadOnlyList<OperationsIncomingPackage> Incoming { get; private init; } = Array.Empty<OperationsIncomingPackage>();

    public bool CanSeePackages { get; private init; }

    public bool CanManage { get; private init; }

    public bool CanSetCredit { get; private init; }

    public static GetOperationsCustomerResponse Success(OperationsCustomerProfile? customer, IReadOnlyList<OperationsIncomingPackage> incoming, bool canSeePackages, bool canManage,
        bool canSetCredit)
    {
        return new GetOperationsCustomerResponse
        {
            Succeeded = true,
            Customer = customer,
            Incoming = incoming,
            CanSeePackages = canSeePackages,
            CanManage = canManage,
            CanSetCredit = canSetCredit
        };
    }

    public static GetOperationsCustomerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCustomerResponse { Error = error };
    }
}
