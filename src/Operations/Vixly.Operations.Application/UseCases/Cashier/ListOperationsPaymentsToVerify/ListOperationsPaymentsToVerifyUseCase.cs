using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.ListOperationsPaymentsToVerify;

// =========================================================
// FILE: ListOperationsPaymentsToVerifyUseCase.cs
//
// PURPOSE:
// Zelle and bank transfers waiting for finance (Slice 5F, C-6): the invoice, customer, amount, date and confirmation number.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.confirm, per warehouse
// - Oldest first, at most 200
// =========================================================
public sealed class ListOperationsPaymentsToVerifyUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsPaymentRepository _payments;

    public ListOperationsPaymentsToVerifyUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsPaymentRepository payments)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<ListOperationsPaymentsToVerifyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsPaymentsToVerifyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsConfirm, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsPaymentsToVerifyResponse.Failure(refused);
        }

        IReadOnlyList<OperationsPayment> pending = await _payments.ListPendingAsync(warehouses.Select(warehouse => warehouse.Id).ToList(), 200, cancellationToken);
        Dictionary<Guid, OperationsInvoice> invoices = new();

        foreach (Guid invoiceId in pending.Select(payment => payment.InvoiceId).Distinct())
        {
            if (await _billing.GetInvoiceAsync(invoiceId, cancellationToken) is { } invoice)
            {
                invoices[invoiceId] = invoice;
            }
        }

        return ListOperationsPaymentsToVerifyResponse.Success(pending, invoices);
    }
}
