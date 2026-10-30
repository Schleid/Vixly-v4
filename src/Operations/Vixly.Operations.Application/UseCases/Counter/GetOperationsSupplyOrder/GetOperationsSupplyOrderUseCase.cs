using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsSupplyOrder;

// =========================================================
// FILE: GetOperationsSupplyOrderUseCase.cs
//
// PURPOSE:
// An order of supplies at the counter (Slice 5E, S-14): its lines, totals, the customer and the discount policy.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.counter.serve, for the warehouse of the order
// =========================================================
public sealed class GetOperationsSupplyOrderUseCase
{
    private readonly OperationsSupplyCounter _counter;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsBillingRepository _billing;

    public GetOperationsSupplyOrderUseCase(
        OperationsSupplyCounter counter,
        IOperationsCustomerRepository customers,
        IOperationsBillingRepository billing)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _counter = counter ?? throw new ArgumentNullException(nameof(counter));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
    }

    public async Task<GetOperationsSupplyOrderResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsSupplyOrderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsSupplyCounter.Opened? opened, Error? refused) = await _counter.SaleAsync(context, request.SaleId, cancellationToken);

        if (refused is not null || opened!.Sale.ShipmentId is not null)
        {
            return GetOperationsSupplyOrderResponse.Failure(refused ?? OperationsSupplySaleErrors.NotFound);
        }

        if (await _customers.GetAsync(opened.Sale.CustomerId, cancellationToken) is not { } customer)
        {
            return GetOperationsSupplyOrderResponse.Failure(OperationsSupplySaleErrors.NotFound);
        }

        return GetOperationsSupplyOrderResponse.Success(opened.Sale, customer, opened.Warehouse,
            opened.Access.CoversWarehouse(OperationsPermissions.DiscountsApprove, opened.Warehouse.Id, opened.Warehouse.BranchId),
            opened.Settings.SupplyDiscountLinePercent, opened.Settings.SupplyDiscountOrderPercent, opened.Settings.SupplyDiscountsAllowed)
            .Invoiced(await _billing.FindOrderInvoiceAsync(opened.Sale.Id, cancellationToken));
    }
}
