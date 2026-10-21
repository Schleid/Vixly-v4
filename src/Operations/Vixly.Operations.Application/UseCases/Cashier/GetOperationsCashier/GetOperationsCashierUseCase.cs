using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashier;

// =========================================================
// FILE: GetOperationsCashierUseCase.cs
//
// PURPOSE:
// The cashier (Slice 5F, C-2): find a customer by phone, email or code, or an invoice by its number or its shipment's or order's; their invoices to pay; the person's open drawers.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.take, per warehouse
// - Invoices of the warehouses where the person takes payments only
// =========================================================
public sealed class GetOperationsCashierUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsPaymentRepository _payments;

    public GetOperationsCashierUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsCustomerRepository customers,
        IOperationsPaymentRepository payments)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<GetOperationsCashierResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCashierRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCashierResponse.Failure(refused);
        }

        List<Guid> ids = warehouses.Select(warehouse => warehouse.Id).ToList();
        IReadOnlyList<OperationsCashDrawer> drawers = (await _payments.ListOpenDrawersAsync(access!.User.Id, cancellationToken)).Where(drawer => ids.Contains(drawer.WarehouseId)).ToList();
        IReadOnlyList<Vixly.Operations.Domain.Customers.OperationsCustomer> matches = Array.Empty<Vixly.Operations.Domain.Customers.OperationsCustomer>();
        Vixly.Operations.Domain.Customers.OperationsCustomer? customer = request.CustomerId is { } chosen ? await _customers.GetAsync(chosen, cancellationToken) : null;

        if (customer is null && request.Find?.Trim() is { Length: > 0 and <= 80 } find)
        {
            // An invoice, shipment or order number finds its customer; otherwise phone, email or customer code.
            if (await _billing.FindByNumberAsync(find.ToUpperInvariant(), cancellationToken) is { } invoice && ids.Contains(invoice.WarehouseId))
            {
                customer = await _customers.GetAsync(invoice.CustomerId, cancellationToken);
            }
            else
            {
                matches = await _customers.FindForCounterAsync(find, 10, cancellationToken);
                customer = matches.Count == 1 ? matches[0] : null;
            }
        }

        IReadOnlyList<OperationsInvoice> open = customer is null ? Array.Empty<OperationsInvoice>() : await _billing.ListOpenInvoicesAsync(customer.Id, ids, cancellationToken);
        return GetOperationsCashierResponse.Success(matches, customer, open, drawers, warehouses);
    }
}
