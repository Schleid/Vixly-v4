using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.WithdrawOperationsQuote;

// =========================================================
// FILE: WithdrawOperationsQuoteUseCase.cs
//
// PURPOSE:
// Withdraws a quote: it no longer counts (Slice 4 design section 7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class WithdrawOperationsQuoteUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public WithdrawOperationsQuoteUseCase(
        OperationsAccess access,
        IOperationsChargeRepository charges,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<WithdrawOperationsQuoteResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        WithdrawOperationsQuoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.QuotesCreate, false, cancellationToken);

        if (refused is not null)
        {
            return WithdrawOperationsQuoteResponse.Failure(refused);
        }

        if (await _charges.GetQuoteAsync(request.QuoteId, cancellationToken) is not { } quote)
        {
            return WithdrawOperationsQuoteResponse.Failure(OperationsChargeErrors.QuoteNotFound);
        }

        // Withdrawn already (a second click): nothing more to do.
        if (quote.Status == OperationsQuoteStatus.Withdrawn)
        {
            return WithdrawOperationsQuoteResponse.Success();
        }

        quote.Withdraw(_timeProvider.GetUtcNow());
        _audit.Record(access!.User.Id, "ops.charges.quote_withdrawn", nameof(OperationsQuote), quote.Id, after: new { number = quote.Number });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return WithdrawOperationsQuoteResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return await _charges.GetQuoteAsync(request.QuoteId, cancellationToken) is { Status: OperationsQuoteStatus.Withdrawn }
            ? WithdrawOperationsQuoteResponse.Success()
            : WithdrawOperationsQuoteResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
