using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Payables;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Payables;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Payables.PayOperationsBill;

// =========================================================
// FILE: PayOperationsBillUseCase.cs
//
// PURPOSE:
// Records a payment the company made on an approved bill (Slice 8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bills.approve with the second check
// - Vixly never sends money: this records a payment made by bank, cheque or otherwise
// =========================================================
public sealed class PayOperationsBillUseCase
{
    private readonly OperationsPayablesDesk _desk;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PayOperationsBillUseCase(
        OperationsPayablesDesk desk,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PayOperationsBillResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PayOperationsBillRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _desk.RequireAsync(context, OperationsPermissions.BillsApprove, false, cancellationToken);

        if (refused is not null)
        {
            return PayOperationsBillResponse.Failure(refused);
        }

        if (await _desk.Payables.GetBillAsync(request.BillId, cancellationToken) is not { } bill)
        {
            return PayOperationsBillResponse.Failure(OperationsPayableErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsBillPayment payment;

        try
        {
            OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            payment = bill.Pay(request.PaidOn, request.Amount, request.Method, request.Reference, access!.User.Id, OperationsTimeZones.Today(utcNow, settings.TimeZone), utcNow);
        }
        catch (DomainException exception)
        {
            return PayOperationsBillResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.bills.paid", nameof(OperationsBill), bill.Id,
            after: new { party = bill.PartyName, bill.Reference, payment.Amount, method = payment.Method.ToString(), paymentReference = payment.Reference, paidOn = payment.PaidOn, owed = bill.Amount - bill.PaidTotal });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return PayOperationsBillResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return PayOperationsBillResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
