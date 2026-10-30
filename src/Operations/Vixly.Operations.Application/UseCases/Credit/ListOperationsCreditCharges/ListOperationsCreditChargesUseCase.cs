using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Credit.ListOperationsCreditCharges;

// =========================================================
// FILE: ListOperationsCreditChargesUseCase.cs
//
// PURPOSE:
// Charges above a limit waiting for the credit department (Slice 5F step 2b, K-3 and K-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.customers.credit
// =========================================================
public sealed class ListOperationsCreditChargesUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCreditRepository _credit;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public ListOperationsCreditChargesUseCase(
        OperationsAccess access,
        IOperationsCreditRepository credit,
        IOperationsCustomerRepository customers,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _credit = credit ?? throw new ArgumentNullException(nameof(credit));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListOperationsCreditChargesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsCreditChargesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersCredit, false, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsCreditChargesResponse.Failure(refused);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        IReadOnlyList<OperationsInvoice> invoices = await _credit.ListAwaitingAsync(200, cancellationToken);
        List<Guid> ids = invoices.Select(invoice => invoice.CustomerId).Distinct().ToList();
        Dictionary<Guid, OperationsCustomer> customers = (await _customers.ListByIdsAsync(ids, cancellationToken)).ToDictionary(customer => customer.Id);
        ILookup<Guid, OperationsInvoice> open = (await _credit.ListAccountInvoicesAsync(ids, cancellationToken)).ToLookup(invoice => invoice.CustomerId);
        DateOnly today = OperationsCreditBook.Today(settings, _timeProvider.GetUtcNow());
        Dictionary<Guid, OperationsCreditRow> accounts = new();

        foreach (Guid id in ids)
        {
            if (await _credit.FindByCustomerAsync(id, cancellationToken) is { } account)
            {
                accounts[id] = new OperationsCreditRow(account, customers.GetValueOrDefault(id), OperationsCreditPosition.Of(account, open[id], today));
            }
        }

        return ListOperationsCreditChargesResponse.Success(invoices, accounts);
    }
}
