using Vixly.Operations.Application.Billing;
using Vixly.Shared.Kernel.Errors;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Credit.ListOperationsCreditCharges;

// =========================================================
// FILE: ListOperationsCreditChargesResponse.cs
//
// PURPOSE:
// Charges above a limit waiting for the credit department (Slice 5F step 2b, K-3 and K-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsCreditChargesResponse
{
    private ListOperationsCreditChargesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsInvoice> Invoices { get; private init; } = Array.Empty<OperationsInvoice>();

    public IReadOnlyDictionary<Guid, OperationsCreditRow> Accounts { get; private init; } = new Dictionary<Guid, OperationsCreditRow>();

    public static ListOperationsCreditChargesResponse Success(IReadOnlyList<OperationsInvoice> invoices, IReadOnlyDictionary<Guid, OperationsCreditRow> accounts)
    {
        return new ListOperationsCreditChargesResponse { Succeeded = true, Invoices = invoices, Accounts = accounts };
    }

    public static ListOperationsCreditChargesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsCreditChargesResponse { Error = error };
    }
}
