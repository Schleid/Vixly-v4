using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.ListOperationsCreditRequests;

// =========================================================
// FILE: ListOperationsCreditRequestsUseCase.cs
//
// PURPOSE:
// The credit notes and voids waiting for a decision in the warehouses a person approves for (Slice 5F, I-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.credits.approve, per warehouse
// - A person's own requests are listed but cannot be decided by them
// =========================================================
public sealed class ListOperationsCreditRequestsUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsUserRepository _users;

    public ListOperationsCreditRequestsUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsUserRepository users)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public async Task<ListOperationsCreditRequestsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsCreditRequestsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.CreditsApprove, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsCreditRequestsResponse.Failure(refused);
        }

        IReadOnlyList<OperationsCreditNote> notes = await _billing.ListRequestsAsync(warehouses.Select(warehouse => warehouse.Id).ToList(), 100, cancellationToken);
        List<OperationsCreditRequestRow> rows = new();
        Dictionary<Guid, string> people = new();

        foreach (OperationsCreditNote note in notes)
        {
            if (await _billing.GetInvoiceAsync(note.InvoiceId, cancellationToken) is not { } invoice)
            {
                continue;
            }

            if (!people.TryGetValue(note.RequestedByUserId, out string? name))
            {
                people[note.RequestedByUserId] = name = (await _users.GetAsync(note.RequestedByUserId, cancellationToken))?.FullName ?? string.Empty;
            }

            rows.Add(new OperationsCreditRequestRow(note.Id, invoice.Id, invoice.Number, OperationsBillingRows.Customer(invoice), note.Kind, note.Total, invoice.CurrencyCode, note.Reason, name,
                note.RequestedUtc, note.RequestedByUserId == access!.User.Id));
        }

        return ListOperationsCreditRequestsResponse.Success(rows);
    }
}
