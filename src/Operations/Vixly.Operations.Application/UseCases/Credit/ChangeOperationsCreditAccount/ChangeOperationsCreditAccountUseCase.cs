using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Credit.ChangeOperationsCreditAccount;

// =========================================================
// FILE: ChangeOperationsCreditAccountUseCase.cs
//
// PURPOSE:
// Change a credit account's limit, terms or state (Slice 5F step 2b, K-1 and K-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.customers.credit with the second check
// - Closed only once nothing is owed or waiting on it
// =========================================================
public sealed class ChangeOperationsCreditAccountUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCreditRepository _credit;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCreditBook _book;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsCreditAccountUseCase(
        OperationsAccess access,
        IOperationsCreditRepository credit,
        IOperationsCustomerRepository customers,
        OperationsCreditBook book,
        OperationsAuditTrail audit,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _credit = credit ?? throw new ArgumentNullException(nameof(credit));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _book = book ?? throw new ArgumentNullException(nameof(book));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsCreditAccountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsCreditAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersCredit, true, cancellationToken);

        if (refused is not null)
        {
            return ChangeOperationsCreditAccountResponse.Failure(refused);
        }

        if (await _credit.FindByCustomerAsync(request.CustomerId, cancellationToken) is not { } account || await _customers.GetAsync(request.CustomerId, cancellationToken) is not { } customer)
        {
            return ChangeOperationsCreditAccountResponse.Failure(OperationsCreditErrors.NotFound);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        (IReadOnlyList<OperationsInvoice> _, OperationsCreditPosition position) = await _book.ReadAsync(account, customer.Id, OperationsCreditBook.Today(settings, utcNow), cancellationToken);
        object before = new { account.Limit, account.TermsDays, status = account.Status.ToString(), account.Reason };

        try
        {
            account.Change(request.Limit, request.TermsDays, request.Status, request.Reason, position.Owed + position.Awaiting, access!.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return ChangeOperationsCreditAccountResponse.Failure(exception.Error);
        }

        // The customer's mark follows the account (K-1).
        if (customer.CreditAllowed != account.IsActive || customer.CreditReason != account.Reason)
        {
            customer.FollowCreditAccount(account.IsActive, account.Reason, utcNow);
        }

        _audit.Record(access.User.Id, "ops.credit.account_changed", nameof(OperationsCreditAccount), account.Id, before: before,
            after: new { account.Limit, account.TermsDays, status = account.Status.ToString(), account.Reason });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsCreditAccountResponse.Success()
            : ChangeOperationsCreditAccountResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
