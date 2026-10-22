using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.DecideOperationsCredit;

// =========================================================
// FILE: DecideOperationsCreditUseCase.cs
//
// PURPOSE:
// Approve or refuse a credit note or a void (Slice 5F, F-6, I-6 and I-7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.credits.approve, at the invoice's warehouse
// - Never the person who asked (the domain refuses it)
// =========================================================
public sealed class DecideOperationsCreditUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly OperationsSupplySales _sales;

    private readonly OperationsNumbering _numbering;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public DecideOperationsCreditUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsSupplyRepository supplies,
        OperationsSupplySales sales,
        OperationsNumbering numbering,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DecideOperationsCreditResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        DecideOperationsCreditRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.CreditsApprove, cancellationToken);

        if (refused is not null)
        {
            return DecideOperationsCreditResponse.Failure(refused);
        }

        if (await _billing.GetCreditNoteAsync(request.CreditNoteId, cancellationToken) is not { } note || warehouses.All(warehouse => warehouse.Id != note.WarehouseId)
            || await _billing.GetInvoiceAsync(note.InvoiceId, cancellationToken) is not { } invoice)
        {
            return DecideOperationsCreditResponse.Failure(OperationsBillingErrors.CreditNotFound);
        }

        // Decided twice (a double click): the same answer, when it is the same decision.
        if (Same(note, access!.User.Id, request.Approve))
        {
            return DecideOperationsCreditResponse.Success(invoice.Id, note.Number);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            if (!request.Approve)
            {
                note.Refuse(request.Note, access!.User.Id, utcNow);
            }
            else
            {
                DateOnly today = OperationsTimeZones.Today(utcNow, (await _settings.FindAsync(cancellationToken))?.TimeZone);
                note.Approve(invoice, await _numbering.NextAsync(OperationsNumberKind.CreditNote, cancellationToken), access!.User.Id, today, utcNow);

                // I-7: a voided order is cancelled and its stock comes back.
                if (note.Kind == OperationsCreditNoteKind.Void && invoice.SupplySaleId is { } saleId && invoice.Kind == OperationsInvoiceKind.SupplyOrder
                    && await _supplies.GetSaleAsync(saleId, cancellationToken) is { } sale)
                {
                    await _sales.CancelAsync(sale, access.User.Id, utcNow, cancellationToken);
                }
            }
        }
        catch (DomainException exception)
        {
            return DecideOperationsCreditResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, request.Approve ? "ops.invoices.credit_approved" : "ops.invoices.credit_refused", nameof(OperationsCreditNote), note.Id,
            after: new { invoice = invoice.Number, number = note.Number, kind = note.Kind.ToString(), total = note.Total, note = note.RefusalNote });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return DecideOperationsCreditResponse.Success(invoice.Id, note.Number);
        }

        // Two clicks at the same instant: the first decided it; the second answers with that decision when it is the same one (review of 5F).
        _unitOfWork.DiscardChanges();
        return await _billing.GetCreditNoteAsync(request.CreditNoteId, cancellationToken) is { } decided && Same(decided, access.User.Id, request.Approve)
            ? DecideOperationsCreditResponse.Success(invoice.Id, decided.Number)
            : DecideOperationsCreditResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private static bool Same(OperationsCreditNote note, Guid userId, bool approve) =>
        note.DecidedByUserId == userId && note.Status == (approve ? OperationsCreditNoteStatus.Approved : OperationsCreditNoteStatus.Refused);
}
