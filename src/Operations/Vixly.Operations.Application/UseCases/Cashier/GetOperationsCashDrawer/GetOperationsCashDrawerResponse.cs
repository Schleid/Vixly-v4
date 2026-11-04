using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashDrawer;

// =========================================================
// FILE: GetOperationsCashDrawerResponse.cs
//
// PURPOSE:
// A cash drawer's report (Slice 5F, C-4): float, cash in, cash out, expected, counted, difference, and each payment and refund in cash.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCashDrawerResponse
{
    private GetOperationsCashDrawerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsCashDrawer Drawer { get; private init; } = null!;

    public IReadOnlyList<OperationsPayment> Payments { get; private init; } = Array.Empty<OperationsPayment>();

    public IReadOnlyList<OperationsRefund> Refunds { get; private init; } = Array.Empty<OperationsRefund>();

    public string Person { get; private init; } = string.Empty;

    public bool Own { get; private init; }

    public static GetOperationsCashDrawerResponse Success(OperationsCashDrawer drawer, IReadOnlyList<OperationsPayment> payments, IReadOnlyList<OperationsRefund> refunds, string person, bool own)
    {
        return new GetOperationsCashDrawerResponse { Succeeded = true, Drawer = drawer, Payments = payments, Refunds = refunds, Person = person, Own = own };
    }

    public static GetOperationsCashDrawerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCashDrawerResponse { Error = error };
    }
}
