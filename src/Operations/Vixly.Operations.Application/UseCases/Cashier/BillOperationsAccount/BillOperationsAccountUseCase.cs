using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.BillOperationsAccount;

// =========================================================
// FILE: BillOperationsAccountUseCase.cs
//
// PURPOSE:
// The cashier bills an invoice to the customer's credit account (Slice 5F step 2b, K-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.take, at the invoice's warehouse
// - Above what is available: refused, or waiting for the credit department, as the company chose (K-2)
// - Two charges at the same instant can together pass the limit (each checked alone); the credit department sees it on the account
// =========================================================
public sealed class BillOperationsAccountUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsCreditRepository _credit;

    private readonly OperationsCreditBook _book;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public BillOperationsAccountUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsCreditRepository credit,
        OperationsCreditBook book,
        OperationsAuditTrail audit,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _credit = credit ?? throw new ArgumentNullException(nameof(credit));
        _book = book ?? throw new ArgumentNullException(nameof(book));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<BillOperationsAccountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        BillOperationsAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (refused is not null)
        {
            return BillOperationsAccountResponse.Failure(refused);
        }

        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice || warehouses.All(warehouse => warehouse.Id != invoice.WarehouseId))
        {
            return BillOperationsAccountResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        // Billed already (a double click): what the first click did.
        if (invoice.OnAccount != OperationsAccountCharge.None && invoice.AccountRequestedByUserId == access!.User.Id)
        {
            return BillOperationsAccountResponse.Success(invoice.OnAccount == OperationsAccountCharge.AwaitingApproval);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());

        if (!settings.AllowCredit)
        {
            return BillOperationsAccountResponse.Failure(OperationsCreditErrors.CreditOff);
        }

        if (await _credit.FindByCustomerAsync(invoice.CustomerId, cancellationToken) is not { IsActive: true } account)
        {
            return BillOperationsAccountResponse.Failure(OperationsCreditErrors.NotActive);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateOnly today = OperationsCreditBook.Today(settings, utcNow);
        (IReadOnlyList<OperationsInvoice> _, OperationsCreditPosition position) = await _book.ReadAsync(account, invoice.CustomerId, today, cancellationToken);
        bool within = invoice.Owed <= position.Available;

        if (!within && settings.CreditOverLimit == OperationsCreditOverLimit.Refuse)
        {
            return BillOperationsAccountResponse.Failure(OperationsCreditErrors.OverLimit);
        }

        try
        {
            invoice.ChargeToAccount(within, today.AddDays(account.TermsOr(settings.CreditTermsDays)), access!.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return BillOperationsAccountResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, within ? "ops.credit.charged" : "ops.credit.charge_requested", nameof(OperationsInvoice), invoice.Id,
            after: new { invoice.Number, amount = invoice.Owed, due = invoice.DueOn, available = position.Available });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? BillOperationsAccountResponse.Success(!within)
            : BillOperationsAccountResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
