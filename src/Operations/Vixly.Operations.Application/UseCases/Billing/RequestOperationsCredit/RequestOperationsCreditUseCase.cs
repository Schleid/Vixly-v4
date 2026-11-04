using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.RequestOperationsCredit;

// =========================================================
// FILE: RequestOperationsCreditUseCase.cs
//
// PURPOSE:
// Ask for a credit note or a void of an invoice, with a reason (Slice 5F, F-6 and I-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.credits.request, at the invoice's warehouse
// - One request waiting per invoice (also a unique index)
// =========================================================
public sealed class RequestOperationsCreditUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RequestOperationsCreditUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RequestOperationsCreditResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RequestOperationsCreditRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.CreditsRequest, cancellationToken);

        if (refused is not null)
        {
            return RequestOperationsCreditResponse.Failure(refused);
        }

        if (request.RequestKey != Guid.Empty && await _billing.FindByRequestKeyAsync(request.RequestKey, cancellationToken) is { } already)
        {
            return RequestOperationsCreditResponse.Success(already.Id);
        }

        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice || warehouses.All(warehouse => warehouse.Id != invoice.WarehouseId))
        {
            return RequestOperationsCreditResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        // One request at a time per invoice, so two never take back the same amount.
        if (await _billing.RequestOpenAsync(invoice.Id, cancellationToken))
        {
            return RequestOperationsCreditResponse.Failure(OperationsBillingErrors.RequestOpen);
        }

        OperationsCreditNote note;
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        Guid key = request.RequestKey == Guid.Empty ? Guid.NewGuid() : request.RequestKey;

        try
        {
            note = request.Void
                ? OperationsCreditNote.RequestVoid(invoice.TenantId, invoice, request.Reason, access!.User.Id, key, utcNow)
                : OperationsCreditNote.RequestCredit(invoice.TenantId, invoice, request.Amounts, request.Reason, access!.User.Id, key, utcNow);
        }
        catch (DomainException exception)
        {
            return RequestOperationsCreditResponse.Failure(exception.Error);
        }

        _billing.Add(note);
        _audit.Record(access.User.Id, "ops.invoices.credit_requested", nameof(OperationsCreditNote), note.Id,
            after: new { invoice = invoice.Number, kind = note.Kind.ToString(), total = note.Total, reason = note.Reason });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RequestOperationsCreditResponse.Success(note.Id);
        }

        // The same form sent twice at the same instant: the first made the request (review of 5F).
        _unitOfWork.DiscardChanges();
        return await _billing.FindByRequestKeyAsync(key, cancellationToken) is { } made
            ? RequestOperationsCreditResponse.Success(made.Id)
            : RequestOperationsCreditResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
