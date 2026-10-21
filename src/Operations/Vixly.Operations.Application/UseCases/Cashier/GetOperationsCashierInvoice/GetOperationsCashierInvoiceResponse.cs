using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashierInvoice;

// =========================================================
// FILE: GetOperationsCashierInvoiceResponse.cs
//
// PURPOSE:
// One invoice at the cashier (Slice 5F, C-2 to C-5): what is owed, the methods the company accepts, the Zelle details and terms, the person's drawer, the payments so far.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCashierInvoiceResponse
{
    private GetOperationsCashierInvoiceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsInvoice Invoice { get; private init; } = null!;

    public Vixly.Operations.Domain.Settings.OperationsTenantSettings Settings { get; private init; } = null!;

    public IReadOnlyList<OperationsPaymentMethod> Methods { get; private init; } = Array.Empty<OperationsPaymentMethod>();

    public OperationsCashDrawer? Drawer { get; private init; }

    public IReadOnlyList<OperationsPayment> Payments { get; private init; } = Array.Empty<OperationsPayment>();

    public IReadOnlyList<OperationsRefund> Refunds { get; private init; } = Array.Empty<OperationsRefund>();

    public bool CanRefund { get; private init; }

    // Slice 5F step 2b (K-3): the customer's credit account and where it stands, when the company allows credit.
    public OperationsCreditAccount? Account { get; private init; }

    public OperationsCreditPosition? Position { get; private init; }

    public static GetOperationsCashierInvoiceResponse Success(OperationsInvoice invoice, Vixly.Operations.Domain.Settings.OperationsTenantSettings settings, IReadOnlyList<OperationsPaymentMethod> methods, OperationsCashDrawer? drawer, IReadOnlyList<OperationsPayment> payments, IReadOnlyList<OperationsRefund> refunds, bool canRefund,
        OperationsCreditAccount? account = null, OperationsCreditPosition? position = null)
    {
        return new GetOperationsCashierInvoiceResponse
        {
            Succeeded = true, Invoice = invoice, Settings = settings, Methods = methods, Drawer = drawer, Payments = payments, Refunds = refunds, CanRefund = canRefund, Account = account,
            Position = position
        };
    }

    public static GetOperationsCashierInvoiceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCashierInvoiceResponse { Error = error };
    }
}
