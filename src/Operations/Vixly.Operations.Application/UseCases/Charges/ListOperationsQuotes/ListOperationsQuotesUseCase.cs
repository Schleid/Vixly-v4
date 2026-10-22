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

namespace Vixly.Operations.Application.UseCases.Charges.ListOperationsQuotes;

// =========================================================
// FILE: ListOperationsQuotesUseCase.cs
//
// PURPOSE:
// The company's latest quotes, with their customers (Slice 4 design section 7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListOperationsQuotesUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsChargeRepository _charges;

    private readonly IOperationsCustomerRepository _customers;

    private readonly TimeProvider _timeProvider;

    public ListOperationsQuotesUseCase(
        OperationsAccess access,
        IOperationsChargeRepository charges,
        IOperationsCustomerRepository customers,
        TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
    }

    public async Task<ListOperationsQuotesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsQuotesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.QuotesCreate, false, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsQuotesResponse.Failure(refused);
        }

        // A quote number typed as is, or the customers the same search finds on the customer list.
        string search = (request.Search ?? string.Empty).Trim();
        search = search.Length > 80 ? search[..80] : search;
        IReadOnlyCollection<Guid>? found = search.Length == 0 ? null : (await _customers.ListAsync(search, 1, 200, cancellationToken)).Items.Select(customer => customer.Id).ToList();
        int page = Math.Max(request.Page, 1);
        int size = Math.Clamp(request.PageSize, 1, 100);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        (IReadOnlyList<OperationsQuote> quotes, int total, OperationsQuoteCounts counts) = await _charges.SearchQuotesAsync(
            new OperationsQuoteFilter(request.State, search.Length == 0 ? null : search.ToUpperInvariant(), found), utcNow, page, size, cancellationToken);
        Dictionary<Guid, OperationsCustomer> customers = (await _customers.ListByIdsAsync(quotes.Select(quote => quote.CustomerId).Distinct().ToList(), cancellationToken))
            .ToDictionary(customer => customer.Id);

        return ListOperationsQuotesResponse.Success(quotes.Select(quote => customers.TryGetValue(quote.CustomerId, out OperationsCustomer? customer)
            ? new OperationsQuoteRow(quote, customer.Code, customer.FullName)
            : new OperationsQuoteRow(quote, null, string.Empty)).ToList(), total, counts, page, size, utcNow);
    }
}
