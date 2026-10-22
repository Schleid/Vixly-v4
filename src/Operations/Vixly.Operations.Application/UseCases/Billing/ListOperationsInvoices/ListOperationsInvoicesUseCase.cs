using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.ListOperationsInvoices;

// =========================================================
// FILE: ListOperationsInvoicesUseCase.cs
//
// PURPOSE:
// The invoices of the warehouses a person sees (Slice 5F, I-8): find by number, customer or shipment, filter by state.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.invoices.view, per warehouse
// - At most 100, newest first
// =========================================================
public sealed class ListOperationsInvoicesUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    public ListOperationsInvoicesUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    public async Task<ListOperationsInvoicesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsInvoicesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.InvoicesView, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsInvoicesResponse.Failure(refused);
        }

        Dictionary<Guid, OperationsWarehouse> seen = warehouses.ToDictionary(warehouse => warehouse.Id);
        OperationsInvoiceFilter filter = Enum.IsDefined(request.Filter) ? request.Filter : OperationsInvoiceFilter.All;
        IReadOnlyList<OperationsInvoice> invoices = await _billing.ListInvoicesAsync(seen.Keys, request.Text, filter, 100, cancellationToken);

        // The approvers see how many requests wait for them.
        IReadOnlyList<OperationsWarehouse> approve = await _desk.WarehousesAsync(access!, OperationsPermissions.CreditsApprove, cancellationToken);
        int requests = approve.Count == 0 ? 0 : (await _billing.ListRequestsAsync(approve.Select(warehouse => warehouse.Id).ToList(), 100, cancellationToken))
            .Count(note => note.RequestedByUserId != access!.User.Id);
        return ListOperationsInvoicesResponse.Success(invoices.Select(invoice => OperationsBillingRows.Row(invoice, seen)).ToList(), requests);
    }
}
