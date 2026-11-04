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

namespace Vixly.Operations.Application.UseCases.Credit.DecideOperationsCreditCharge;

// =========================================================
// FILE: DecideOperationsCreditChargeUseCase.cs
//
// PURPOSE:
// The credit department approves or refuses a charge above the limit (Slice 5F step 2b, K-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.customers.credit
// - A person never decides a charge they asked for
// =========================================================
public sealed class DecideOperationsCreditChargeUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsCreditRepository _credit;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public DecideOperationsCreditChargeUseCase(
        OperationsAccess access,
        IOperationsBillingRepository billing,
        IOperationsCreditRepository credit,
        OperationsAuditTrail audit,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _credit = credit ?? throw new ArgumentNullException(nameof(credit));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DecideOperationsCreditChargeResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        DecideOperationsCreditChargeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersCredit, true, cancellationToken);

        if (refused is not null)
        {
            return DecideOperationsCreditChargeResponse.Failure(refused);
        }

        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice)
        {
            return DecideOperationsCreditChargeResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        // Decided already (a double click with the same decision): done.
        if (invoice.OnAccount != OperationsAccountCharge.AwaitingApproval && invoice.AccountDecidedByUserId == access!.User.Id
            && (invoice.OnAccount == OperationsAccountCharge.Charged) == request.Approve)
        {
            return DecideOperationsCreditChargeResponse.Success();
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        OperationsCreditAccount? account = await _credit.FindByCustomerAsync(invoice.CustomerId, cancellationToken);

        if (request.Approve && account is not { IsActive: true })
        {
            return DecideOperationsCreditChargeResponse.Failure(OperationsCreditErrors.NotActive);
        }

        string? note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        if ((!request.Approve && note is null) || note is { Length: > OperationsBillingLimits.ReasonMaxLength })
        {
            return DecideOperationsCreditChargeResponse.Failure(OperationsCreditErrors.ReasonRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            invoice.DecideCharge(request.Approve, OperationsCreditBook.Today(settings, utcNow).AddDays(account?.TermsOr(settings.CreditTermsDays) ?? settings.CreditTermsDays), access!.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return DecideOperationsCreditChargeResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, request.Approve ? "ops.credit.charge_approved" : "ops.credit.charge_refused", nameof(OperationsInvoice), invoice.Id,
            after: new { invoice.Number, amount = invoice.Owed, due = invoice.DueOn }, reason: note);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? DecideOperationsCreditChargeResponse.Success()
            : DecideOperationsCreditChargeResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
