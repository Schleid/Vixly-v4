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

namespace Vixly.Operations.Application.UseCases.Cashier.TakeOperationsPayment;

// =========================================================
// FILE: TakeOperationsPaymentUseCase.cs
//
// PURPOSE:
// Take the payment of an invoice in full, with one or more methods (Slice 5F, F-4, C-3 to C-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.take, at the invoice's warehouse
// - Cash only into the person's open drawer at that warehouse
// =========================================================
public sealed class TakeOperationsPaymentUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsPaymentRepository _payments;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsScreenRepository _screens;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public TakeOperationsPaymentUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsPaymentRepository payments,
        IOperationsSettingsRepository settings,
        IOperationsScreenRepository screens,
        IOperationsSecretGenerator secrets,
        OperationsNumbering numbering,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _screens = screens ?? throw new ArgumentNullException(nameof(screens));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<TakeOperationsPaymentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        TakeOperationsPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (refused is not null)
        {
            return TakeOperationsPaymentResponse.Failure(refused);
        }

        if (request.RequestKey != Guid.Empty && await _payments.FindPaymentByKeyAsync(request.RequestKey, cancellationToken) is { } already)
        {
            return TakeOperationsPaymentResponse.Success(already.Id);
        }

        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice || warehouses.All(warehouse => warehouse.Id != invoice.WarehouseId))
        {
            return TakeOperationsPaymentResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);

        if (request.Tenders.Any(tender => tender.Amount != 0m && !settings.Accepts(tender.Method)))
        {
            return TakeOperationsPaymentResponse.Failure(OperationsPaymentErrors.MethodNotAccepted);
        }

        OperationsCashDrawer? drawer = await _payments.FindOpenDrawerAsync(access!.User.Id, invoice.WarehouseId, cancellationToken);
        Guid key = request.RequestKey == Guid.Empty ? Guid.NewGuid() : request.RequestKey;
        OperationsPayment payment;

        // Slice 5F step 3 (P-9): the customer's own acceptance on the customer screen counts; the clerk's tick stays for counters without one.
        bool zelleTermsAccepted = request.ZelleTermsAccepted
            || (request.Tenders.Any(tender => tender.Method == OperationsPaymentMethod.Zelle)
                && await _screens.ZelleAcceptedAsync(invoice.Id, _secrets.HashToken(settings.ZelleTerms(invoice.Language) ?? string.Empty),
                    utcNow - Vixly.Operations.Domain.Screens.OperationsScreenRecord.ZelleTermsLifetime, cancellationToken) is not null);

        try
        {
            payment = OperationsPayment.Take(invoice.TenantId, invoice, await _numbering.NextAsync(OperationsNumberKind.Payment, cancellationToken), request.Tenders, drawer,
                zelleTermsAccepted, access.User.Id, key, utcNow);
        }
        catch (DomainException exception)
        {
            return TakeOperationsPaymentResponse.Failure(exception.Error);
        }

        _payments.Add(payment);
        _audit.Record(access.User.Id, "ops.payments.taken", nameof(OperationsPayment), payment.Id,
            after: new { invoice = invoice.Number, number = payment.Number, total = payment.Total, tenders = payment.Tenders.Select(tender => new { method = tender.Method.ToString(), tender.Amount }) });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return TakeOperationsPaymentResponse.Success(payment.Id);
        }

        // Sent twice at the same instant: the first took it; otherwise the invoice changed meanwhile.
        _unitOfWork.DiscardChanges();
        return await _payments.FindPaymentByKeyAsync(key, cancellationToken) is { } made
            ? TakeOperationsPaymentResponse.Success(made.Id)
            : TakeOperationsPaymentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
