using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoice;

// =========================================================
// FILE: GetOperationsInvoiceUseCase.cs
//
// PURPOSE:
// One invoice with its lines, taxes and credit notes (Slice 5F, I-2, I-6 and I-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.invoices.view, per warehouse (the invoice's)
// =========================================================
public sealed class GetOperationsInvoiceUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsUserRepository _users;

    public GetOperationsInvoiceUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsLocationRepository locations,
        IOperationsUserRepository users)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public async Task<GetOperationsInvoiceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> seen, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.InvoicesView, cancellationToken);

        // An approver sees the invoice they decide on, even without the right to see invoices (review of 5F).
        IReadOnlyList<OperationsWarehouse> warehouses = access is null ? seen
            : seen.Concat(await _desk.WarehousesAsync(access, OperationsPermissions.CreditsApprove, cancellationToken)).ToList();

        if (access is null || warehouses.Count == 0)
        {
            return GetOperationsInvoiceResponse.Failure(refused ?? OperationsAccessErrors.PermissionRequired);
        }

        // Another warehouse's invoice answers "not found".
        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice || warehouses.All(warehouse => warehouse.Id != invoice.WarehouseId))
        {
            return GetOperationsInvoiceResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        OperationsWarehouse? at = await _locations.GetWarehouseAsync(invoice.WarehouseId, cancellationToken);
        IReadOnlyList<OperationsCreditNote> notes = await _billing.ListCreditNotesAsync(invoice.Id, cancellationToken);
        Dictionary<Guid, string> people = new();

        foreach (Guid userId in notes.SelectMany(note => new[] { note.RequestedByUserId, note.DecidedByUserId ?? Guid.Empty }).Where(id => id != Guid.Empty).Distinct())
        {
            people[userId] = (await _users.GetAsync(userId, cancellationToken))?.FullName ?? string.Empty;
        }

        return GetOperationsInvoiceResponse.Success(invoice, notes, people,
            at is not null && access!.CoversWarehouse(OperationsPermissions.CreditsRequest, at.Id, at.BranchId),
            at is not null && access!.CoversWarehouse(OperationsPermissions.CreditsApprove, at.Id, at.BranchId),
            access!.User.Id);
    }
}
