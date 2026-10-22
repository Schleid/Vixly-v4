using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsSupplyOrder;

// =========================================================
// FILE: GetOperationsSupplyOrderResponse.cs
//
// PURPOSE:
// An order of supplies at the counter (Slice 5E, S-14): its lines, totals, the customer and the discount policy.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsSupplyOrderResponse
{
    private GetOperationsSupplyOrderResponse()
    {
    }

    public bool Succeeded { get; private init; }

    // Slice 5F: its invoice once issued.
    public Vixly.Operations.Domain.Billing.OperationsInvoice? Invoice { get; private set; }

    internal GetOperationsSupplyOrderResponse Invoiced(Vixly.Operations.Domain.Billing.OperationsInvoice? invoice)
    {
        Invoice = invoice;
        return this;
    }

    public Error? Error { get; private init; }

    public OperationsSupplySale Sale { get; private init; } = null!;

    public Vixly.Operations.Domain.Customers.OperationsCustomer Customer { get; private init; } = null!;

    public Vixly.Operations.Domain.Locations.OperationsWarehouse Warehouse { get; private init; } = null!;

    public bool CanApprove { get; private init; }

    public decimal LinePercent { get; private init; }

    public decimal OrderPercent { get; private init; }

    public bool DiscountsAllowed { get; private init; }

    public static GetOperationsSupplyOrderResponse Success(OperationsSupplySale sale, Vixly.Operations.Domain.Customers.OperationsCustomer customer, Vixly.Operations.Domain.Locations.OperationsWarehouse warehouse, bool canApprove, decimal linePercent, decimal orderPercent, bool discountsAllowed)
    {
        return new GetOperationsSupplyOrderResponse { Succeeded = true, Sale = sale, Customer = customer, Warehouse = warehouse, CanApprove = canApprove, LinePercent = linePercent, OrderPercent = orderPercent, DiscountsAllowed = discountsAllowed };
    }

    public static GetOperationsSupplyOrderResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsSupplyOrderResponse { Error = error };
    }
}
