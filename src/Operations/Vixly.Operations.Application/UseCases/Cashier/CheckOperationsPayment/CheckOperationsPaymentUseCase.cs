using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.CheckOperationsPayment;

// =========================================================
// FILE: CheckOperationsPaymentUseCase.cs
//
// PURPOSE:
// Finance's check of a Zelle or transfer (Slice 5F, C-6): received settles it; not received takes it back, the invoice is owed again and the customer is told.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.confirm, at the payment's warehouse
// - Who and when are kept on the tender
// =========================================================
public sealed class CheckOperationsPaymentUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsPaymentRepository _payments;

    private readonly OperationsOutboxWriter _outbox;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CheckOperationsPaymentUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsPaymentRepository payments,
        OperationsOutboxWriter outbox,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CheckOperationsPaymentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CheckOperationsPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsConfirm, cancellationToken);

        if (refused is not null)
        {
            return CheckOperationsPaymentResponse.Failure(refused);
        }

        if (await _payments.GetPaymentAsync(request.PaymentId, cancellationToken) is not { } payment || warehouses.All(warehouse => warehouse.Id != payment.WarehouseId)
            || await _billing.GetInvoiceAsync(payment.InvoiceId, cancellationToken) is not { } invoice)
        {
            return CheckOperationsPaymentResponse.Failure(OperationsPaymentErrors.NotFound);
        }

        // Checked twice the same way (a double click): done.
        if (payment.Tenders.FirstOrDefault(tender => tender.Id == request.TenderId) is { } seen && seen.CheckedByUserId == access!.User.Id
            && seen.Status == (request.Received ? OperationsTenderStatus.Settled : OperationsTenderStatus.NotReceived))
        {
            return CheckOperationsPaymentResponse.Success();
        }

        // Another person confirms the money arrived (P-6): never the one who took the payment (review of 5F).
        if (payment.TakenByUserId == access!.User.Id)
        {
            return CheckOperationsPaymentResponse.Failure(OperationsPaymentErrors.OwnPayment);
        }

        OperationsPaymentTender tender;

        try
        {
            tender = payment.Check(invoice, request.TenderId, request.Received, request.Note, access!.User.Id, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return CheckOperationsPaymentResponse.Failure(exception.Error);
        }

        if (!request.Received)
        {
            _outbox.Write(OperationsPaymentNotice.MessageType, context.RequiredTenantId, payment.Id, new OperationsPaymentNotice(invoice.CustomerId, invoice.Id, request.PortalSlug));
        }

        _audit.Record(access.User.Id, request.Received ? "ops.payments.received" : "ops.payments.not_received", nameof(OperationsPayment), payment.Id,
            after: new { invoice = invoice.Number, number = payment.Number, method = tender.Method.ToString(), tender.Amount, note = tender.CheckNote });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CheckOperationsPaymentResponse.Success()
            : CheckOperationsPaymentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
