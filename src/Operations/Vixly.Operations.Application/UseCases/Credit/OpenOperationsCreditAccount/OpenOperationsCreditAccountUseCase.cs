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

namespace Vixly.Operations.Application.UseCases.Credit.OpenOperationsCreditAccount;

// =========================================================
// FILE: OpenOperationsCreditAccountUseCase.cs
//
// PURPOSE:
// Open a credit account for a customer (Slice 5F step 2b, K-1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.customers.credit with the second check
// - One account per customer (unique index)
// =========================================================
public sealed class OpenOperationsCreditAccountUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCreditRepository _credit;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OpenOperationsCreditAccountUseCase(
        OperationsAccess access,
        IOperationsCreditRepository credit,
        IOperationsCustomerRepository customers,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _credit = credit ?? throw new ArgumentNullException(nameof(credit));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<OpenOperationsCreditAccountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        OpenOperationsCreditAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersCredit, true, cancellationToken);

        if (refused is not null)
        {
            return OpenOperationsCreditAccountResponse.Failure(refused);
        }

        OperationsCustomer? customer = request.CustomerId is { } id ? await _customers.GetAsync(id, cancellationToken)
            : string.IsNullOrWhiteSpace(request.CustomerCode) ? null : await _customers.FindByCodeAsync(request.CustomerCode.Trim().ToUpperInvariant(), cancellationToken);

        if (customer is null)
        {
            return OpenOperationsCreditAccountResponse.Failure(OperationsCreditErrors.NotFound);
        }

        // Open already (a double click, or opened before): the same account.
        if (await _credit.FindByCustomerAsync(customer.Id, cancellationToken) is not null)
        {
            return OpenOperationsCreditAccountResponse.Success(customer.Id);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsCreditAccount account;

        try
        {
            account = OperationsCreditAccount.Open(context.RequiredTenantId, customer.Id, request.Limit, request.TermsDays, access!.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return OpenOperationsCreditAccountResponse.Failure(exception.Error);
        }

        // Its first statement notice comes on the 1st of next month.
        account.StatementSent(OperationsCreditBook.Today(await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow), utcNow)
            .ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture), utcNow);
        _credit.Add(account);
        customer.FollowCreditAccount(true, null, utcNow);
        _audit.Record(access.User.Id, "ops.credit.account_opened", nameof(OperationsCreditAccount), account.Id, after: new { customer = customer.Id, account.Limit, account.TermsDays });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return OpenOperationsCreditAccountResponse.Success(customer.Id);
        }

        _unitOfWork.DiscardChanges();
        return await _credit.FindByCustomerAsync(customer.Id, cancellationToken) is not null
            ? OpenOperationsCreditAccountResponse.Success(customer.Id)
            : OpenOperationsCreditAccountResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
