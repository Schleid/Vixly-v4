using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashier;

// =========================================================
// FILE: GetOperationsCashierResponse.cs
//
// PURPOSE:
// The cashier (Slice 5F, C-2): find a customer by phone, email or code, or an invoice by its number or its shipment's or order's; their invoices to pay; the person's open drawers.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCashierResponse
{
    private GetOperationsCashierResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<Vixly.Operations.Domain.Customers.OperationsCustomer> Matches { get; private init; } = Array.Empty<Vixly.Operations.Domain.Customers.OperationsCustomer>();

    public Vixly.Operations.Domain.Customers.OperationsCustomer? Customer { get; private init; }

    public IReadOnlyList<OperationsInvoice> Invoices { get; private init; } = Array.Empty<OperationsInvoice>();

    public IReadOnlyList<OperationsCashDrawer> Drawers { get; private init; } = Array.Empty<OperationsCashDrawer>();

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public static GetOperationsCashierResponse Success(IReadOnlyList<Vixly.Operations.Domain.Customers.OperationsCustomer> matches, Vixly.Operations.Domain.Customers.OperationsCustomer? customer, IReadOnlyList<OperationsInvoice> invoices, IReadOnlyList<OperationsCashDrawer> drawers, IReadOnlyList<OperationsWarehouse> warehouses)
    {
        return new GetOperationsCashierResponse { Succeeded = true, Matches = matches, Customer = customer, Invoices = invoices, Drawers = drawers, Warehouses = warehouses };
    }

    public static GetOperationsCashierResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCashierResponse { Error = error };
    }
}
