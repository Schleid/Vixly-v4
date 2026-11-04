using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.EmailOperationsQuote;

// =========================================================
// FILE: EmailOperationsQuoteUseCase.cs
//
// PURPOSE:
// Emails a quote to its customer, with the PDF attached (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Only an open quote still within its date; to the customer's own email, in their language
// - Who sent it and to which address are audited
//
// IMPORTANT NOTES:
// - Sent at once: the clerk sees whether it went
// =========================================================
public sealed class EmailOperationsQuoteUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsQuoteDocuments _documents;

    private readonly IOperationsCustomerMailer _mailer;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public EmailOperationsQuoteUseCase(OperationsAccess access, IOperationsChargeRepository charges, OperationsQuoteDocuments documents, IOperationsCustomerMailer mailer,
        OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<EmailOperationsQuoteResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        EmailOperationsQuoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.QuotesCreate, false, cancellationToken);

        if (refused is not null)
        {
            return EmailOperationsQuoteResponse.Failure(refused);
        }

        if (await _charges.GetQuoteAsync(request.QuoteId, cancellationToken) is not { } quote)
        {
            return EmailOperationsQuoteResponse.Failure(OperationsChargeErrors.QuoteNotFound);
        }

        // A withdrawn or past quote is not offered again.
        if (!quote.IsValid(_timeProvider.GetUtcNow()))
        {
            return EmailOperationsQuoteResponse.Failure(OperationsChargeErrors.QuoteNotValid);
        }

        if (await _documents.RenderAsync(quote, cancellationToken) is not { } drawn)
        {
            return EmailOperationsQuoteResponse.Failure(OperationsChargeErrors.QuoteNotFound);
        }

        string address = drawn.Customer.Email.Value;

        try
        {
            await _mailer.SendAsync(new OperationsCustomerMail(OperationsCustomerMailKind.Quote, address, drawn.Customer.FullName, drawn.Customer.PreferredLanguage, drawn.Company,
                string.Empty)
            {
                Extra = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["number"] = quote.Number,
                    ["date"] = drawn.ValidUntil.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                },
                Attachment = (quote.Number + ".pdf", "application/pdf", drawn.Pdf)
            }, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return EmailOperationsQuoteResponse.Failure(OperationsChargeErrors.QuoteEmailFailed);
        }

        _audit.Record(access!.User.Id, "ops.quotes.emailed", nameof(OperationsQuote), quote.Id, arguments: new { number = quote.Number });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return EmailOperationsQuoteResponse.Success(address, quote.Number);
    }
}
