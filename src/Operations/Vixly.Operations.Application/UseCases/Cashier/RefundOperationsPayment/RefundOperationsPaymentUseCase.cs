using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.RefundOperationsPayment;

// =========================================================
// FILE: RefundOperationsPaymentUseCase.cs
//
// PURPOSE:
// Give money back after a credit note on a paid invoice (Slice 5F, F-7 and C-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.refund, at the invoice's warehouse
// - Cash comes out of the person's open drawer
// =========================================================
public sealed class RefundOperationsPaymentUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsPaymentRepository _payments;

    private readonly OperationsNumbering _numbering;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RefundOperationsPaymentUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsPaymentRepository payments,
        OperationsNumbering numbering,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RefundOperationsPaymentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RefundOperationsPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsRefund, cancellationToken);

        if (refused is not null)
        {
            return RefundOperationsPaymentResponse.Failure(refused);
        }

        if (request.RequestKey != Guid.Empty && await _payments.FindRefundByKeyAsync(request.RequestKey, cancellationToken) is { } already)
        {
            return RefundOperationsPaymentResponse.Success(already.Id);
        }

        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice || warehouses.All(warehouse => warehouse.Id != invoice.WarehouseId))
        {
            return RefundOperationsPaymentResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        // Given back only by a method the company uses (review of 5F).
        if ((await _settings.FindAsync(cancellationToken)) is { } settings && !settings.Accepts(request.Method))
        {
            return RefundOperationsPaymentResponse.Failure(OperationsPaymentErrors.MethodNotAccepted);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsCashDrawer? drawer = await _payments.FindOpenDrawerAsync(access!.User.Id, invoice.WarehouseId, cancellationToken);
        Guid key = request.RequestKey == Guid.Empty ? Guid.NewGuid() : request.RequestKey;
        OperationsRefund refund;

        try
        {
            refund = OperationsRefund.Give(invoice.TenantId, invoice, await _numbering.NextAsync(OperationsNumberKind.Payment, cancellationToken), request.Method, request.Amount,
                request.Reason, request.Reference, drawer, access.User.Id, key, utcNow);
        }
        catch (DomainException exception)
        {
            return RefundOperationsPaymentResponse.Failure(exception.Error);
        }

        _payments.Add(refund);
        _audit.Record(access.User.Id, "ops.payments.refunded", nameof(OperationsRefund), refund.Id,
            after: new { invoice = invoice.Number, number = refund.Number, method = refund.Method.ToString(), refund.Amount, refund.Reason });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RefundOperationsPaymentResponse.Success(refund.Id);
        }

        _unitOfWork.DiscardChanges();
        return await _payments.FindRefundByKeyAsync(key, cancellationToken) is { } made
            ? RefundOperationsPaymentResponse.Success(made.Id)
            : RefundOperationsPaymentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
