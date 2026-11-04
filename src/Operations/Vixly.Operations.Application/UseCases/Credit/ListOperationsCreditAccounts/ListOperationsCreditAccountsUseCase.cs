using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Credit.ListOperationsCreditAccounts;

// =========================================================
// FILE: ListOperationsCreditAccountsUseCase.cs
//
// PURPOSE:
// The credit department's accounts (Slice 5F step 2b, K-6): each with its customer, what is owed, available and overdue.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.customers.credit (the credit department)
// =========================================================
public sealed class ListOperationsCreditAccountsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCreditRepository _credit;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public ListOperationsCreditAccountsUseCase(
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

    public async Task<ListOperationsCreditAccountsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsCreditAccountsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersCredit, false, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsCreditAccountsResponse.Failure(refused);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        IReadOnlyList<OperationsCreditAccount> accounts = await _credit.ListAsync(500, cancellationToken);
        List<Guid> ids = accounts.Select(account => account.CustomerId).ToList();
        Dictionary<Guid, OperationsCustomer> customers = (await _customers.ListByIdsAsync(ids, cancellationToken)).ToDictionary(customer => customer.Id);
        ILookup<Guid, OperationsInvoice> invoices = (await _credit.ListAccountInvoicesAsync(ids, cancellationToken)).ToLookup(invoice => invoice.CustomerId);
        DateOnly today = OperationsCreditBook.Today(settings, _timeProvider.GetUtcNow());
        List<OperationsCreditRow> rows = accounts.Select(account => new OperationsCreditRow(account, customers.GetValueOrDefault(account.CustomerId),
            OperationsCreditPosition.Of(account, invoices[account.CustomerId], today))).ToList();

        return ListOperationsCreditAccountsResponse.Success(rows, (await _credit.ListAwaitingAsync(500, cancellationToken)).Count, settings.AllowCredit, settings.CurrencyCode,
            await _credit.ListMarkedWithoutAccountAsync(100, cancellationToken));
    }
}
