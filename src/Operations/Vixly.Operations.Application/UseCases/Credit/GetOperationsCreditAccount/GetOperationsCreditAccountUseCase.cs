using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditAccount;

// =========================================================
// FILE: GetOperationsCreditAccountUseCase.cs
//
// PURPOSE:
// A customer's credit account (Slice 5F step 2b, K-6): its terms, its open items with their age, and what can be changed.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.customers.credit
// =========================================================
public sealed class GetOperationsCreditAccountUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCreditRepository _credit;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCreditBook _book;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsCreditAccountUseCase(
        OperationsAccess access,
        IOperationsCreditRepository credit,
        IOperationsCustomerRepository customers,
        OperationsCreditBook book,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _credit = credit ?? throw new ArgumentNullException(nameof(credit));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _book = book ?? throw new ArgumentNullException(nameof(book));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsCreditAccountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCreditAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersCredit, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCreditAccountResponse.Failure(refused);
        }

        if (await _customers.GetAsync(request.CustomerId, cancellationToken) is not { } customer)
        {
            return GetOperationsCreditAccountResponse.Failure(OperationsCreditErrors.NotFound);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        OperationsCreditAccount? account = await _credit.FindByCustomerAsync(customer.Id, cancellationToken);
        DateOnly today = OperationsCreditBook.Today(settings, _timeProvider.GetUtcNow());
        (IReadOnlyList<OperationsInvoice> items, OperationsCreditPosition position) = await _book.ReadAsync(account, customer.Id, today, cancellationToken);
        return GetOperationsCreditAccountResponse.Success(customer, account, items, position, settings.CreditTermsDays, settings.CurrencyCode, today, settings.AllowCredit);
    }
}
