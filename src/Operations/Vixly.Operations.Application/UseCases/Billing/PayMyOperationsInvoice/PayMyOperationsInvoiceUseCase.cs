using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Billing.PayMyOperationsInvoice;

// =========================================================
// FILE: PayMyOperationsInvoiceUseCase.cs
//
// PURPOSE:
// A customer pays an invoice on the portal by Zelle or bank transfer (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Show what to pay and how (Zelle: the payee and the terms; bank transfer: the company's instructions)
// - Record "sent" for the whole amount left, with the reference, waiting for finance (Payments to verify)
//
// IMPORTANT NOTES:
// - Only the methods the company offers on the portal and has set up (Settings → Payment methods)
// - The whole amount left, never a part (owner, 2026-10-27); the customer's own invoice only (S-3)
// - Card is paid on Stripe's page (PayMyOperationsInvoiceByCard)
// =========================================================
public sealed class PayMyOperationsInvoiceUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsPaymentRepository _payments;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public PayMyOperationsInvoiceUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsBillingRepository billing,
        IOperationsPaymentRepository payments,
        IOperationsSettingsRepository settings,
        OperationsNumbering numbering,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    // The invoice can be paid this way now: issued, something owed, nothing already waiting, not billed to a credit account.
    public static bool Payable(OperationsInvoice invoice) =>
        invoice.Status == OperationsInvoiceStatus.Issued && invoice.Owed > 0m && invoice.PendingTotal == 0m && invoice.OnAccount == OperationsAccountCharge.None;

    public async Task<PayMyOperationsInvoiceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PayMyOperationsInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return PayMyOperationsInvoiceResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice || invoice.CustomerId != signedIn.Customer.Id)
        {
            return PayMyOperationsInvoiceResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        // Sent twice: what the first did.
        if (request.Report && request.RequestKey != Guid.Empty && await _payments.FindPaymentByKeyAsync(request.RequestKey, cancellationToken) is { } already
            && already.InvoiceId == invoice.Id)
        {
            return PayMyOperationsInvoiceResponse.Reported(invoice.Number, already.Number);
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (request.Method is not (OperationsPaymentMethod.Zelle or OperationsPaymentMethod.BankTransfer) || settings?.OffersOnPortal(request.Method, false) != true)
        {
            return PayMyOperationsInvoiceResponse.Failure(OperationsPaymentErrors.MethodNotAccepted);
        }

        if (!Payable(invoice))
        {
            return PayMyOperationsInvoiceResponse.Failure(OperationsPaymentErrors.NothingOwed);
        }

        string language = signedIn.Customer.PreferredLanguage.Value;

        if (!request.Report)
        {
            return request.Method == OperationsPaymentMethod.Zelle
                ? PayMyOperationsInvoiceResponse.Success(invoice.Number, invoice.Owed, invoice.CurrencyCode, settings.ZelleTerms(language), settings.ZelleTag, settings.ZelleContact,
                    settings.ZelleName, settings.ZelleQrKey is not null)
                : PayMyOperationsInvoiceResponse.Success(invoice.Number, invoice.Owed, invoice.CurrencyCode, settings.BankTransferInstructions(language), null, null, null, false);
        }

        OperationsPayment payment;

        try
        {
            payment = OperationsPayment.Report(invoice.TenantId, invoice, await _numbering.NextAsync(OperationsNumberKind.Payment, cancellationToken), request.Method, request.Reference,
                request.TermsAccepted, request.RequestKey == Guid.Empty ? Guid.NewGuid() : request.RequestKey, _accounts.UtcNow);
        }
        catch (DomainException exception)
        {
            return PayMyOperationsInvoiceResponse.Failure(exception.Error);
        }

        _payments.Add(payment);
        _audit.Record(null, "ops.payments.reported_by_customer", nameof(OperationsPayment), payment.Id,
            after: new { invoice = invoice.Number, method = request.Method.ToString(), payment.Total, customer = signedIn.Customer.Code });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? PayMyOperationsInvoiceResponse.Reported(invoice.Number, payment.Number)
            : PayMyOperationsInvoiceResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
    }
}
