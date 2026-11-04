using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.GetOperationsQuoteDocument;

// =========================================================
// FILE: GetOperationsQuoteDocumentUseCase.cs
//
// PURPOSE:
// The PDF of a quote, to print (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Staff who make quotes; any of the company's quotes, open, withdrawn or past its date
//
// IMPORTANT NOTES:
// - In the customer's language, with the company's document logo
// =========================================================
public sealed class GetOperationsQuoteDocumentUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsQuoteDocuments _documents;

    public GetOperationsQuoteDocumentUseCase(OperationsAccess access, IOperationsChargeRepository charges, OperationsQuoteDocuments documents)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
    }

    public async Task<GetOperationsQuoteDocumentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsQuoteDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.QuotesCreate, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsQuoteDocumentResponse.Failure(refused);
        }

        if (await _charges.GetQuoteAsync(request.QuoteId, cancellationToken) is not { } quote
            || await _documents.RenderAsync(quote, cancellationToken) is not { } drawn)
        {
            return GetOperationsQuoteDocumentResponse.Failure(OperationsChargeErrors.QuoteNotFound);
        }

        return GetOperationsQuoteDocumentResponse.Success(drawn.Pdf, quote.Number + ".pdf");
    }
}
