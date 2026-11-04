using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsChargeRepository.cs
//
// PURPOSE:
// EF Core store of rate cards, extras, the insurance plan and quotes (Slice 4).
//
// RESPONSIBILITIES:
// - Implement IOperationsChargeRepository
//
// IMPORTANT NOTES:
// - Lists are read without tracking and bounded
// =========================================================
internal sealed class OperationsChargeRepository : IOperationsChargeRepository
{
    private readonly OperationsDbContext _context;

    public OperationsChargeRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsRateCard card) => _context.RateCards.Add(card ?? throw new ArgumentNullException(nameof(card)));

    public void Add(OperationsChargeExtra extra) => _context.ChargeExtras.Add(extra ?? throw new ArgumentNullException(nameof(extra)));

    public void Add(OperationsInsurancePlan plan) => _context.InsurancePlans.Add(plan ?? throw new ArgumentNullException(nameof(plan)));

    public void Add(OperationsQuote quote) => _context.Quotes.Add(quote ?? throw new ArgumentNullException(nameof(quote)));

    public Task<OperationsRateCard?> GetCardAsync(Guid cardId, CancellationToken cancellationToken) =>
        _context.RateCards.FirstOrDefaultAsync(card => card.Id == cardId, cancellationToken);

    public Task<OperationsRateCard?> FindCardAsync(string serviceCode, Guid? originWarehouseId, Guid? destinationWarehouseId, Guid? customerId, CancellationToken cancellationToken) =>
        _context.RateCards.AsNoTracking().FirstOrDefaultAsync(card => card.ServiceCode == serviceCode && card.OriginWarehouseId == originWarehouseId
            && card.DestinationWarehouseId == destinationWarehouseId && card.CustomerId == customerId, cancellationToken);

    public void Add(OperationsItemType itemType) => _context.ItemTypes.Add(itemType ?? throw new ArgumentNullException(nameof(itemType)));

    public Task<OperationsItemType?> GetItemTypeAsync(Guid itemTypeId, CancellationToken cancellationToken) =>
        _context.ItemTypes.FirstOrDefaultAsync(type => type.Id == itemTypeId, cancellationToken);

    public async Task<IReadOnlyList<OperationsItemType>> ListItemTypesAsync(CancellationToken cancellationToken) =>
        await _context.ItemTypes.AsNoTracking().OrderBy(type => type.Name).Take(OperationsChargeLimits.MaxItemTypes).ToListAsync(cancellationToken);

    public Task<bool> AnyCardAsync(CancellationToken cancellationToken) => _context.RateCards.AnyAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsRateCard>> ListCardsAsync(CancellationToken cancellationToken) =>
        await _context.RateCards.AsNoTracking().OrderBy(card => card.ServiceCode).ThenBy(card => card.CustomerId).ThenBy(card => card.OriginWarehouseId).ThenBy(card => card.DestinationWarehouseId)
            .Take(OperationsChargeLimits.MaxCards).ToListAsync(cancellationToken);

    public Task<bool> AnyPublishedAsync(CancellationToken cancellationToken) =>
        _context.RateCards.AnyAsync(card => card.IsActive && card.Published && card.CustomerId == null, cancellationToken);

    public async Task<IReadOnlyList<OperationsRateCard>> ListCardsForAsync(Guid? customerId, CancellationToken cancellationToken) =>
        await _context.RateCards.AsNoTracking().Where(card => card.IsActive && (card.CustomerId == null || card.CustomerId == customerId))
            .Take(OperationsChargeLimits.MaxCards).ToListAsync(cancellationToken);

    public Task<OperationsChargeExtra?> GetExtraAsync(Guid extraId, CancellationToken cancellationToken) =>
        _context.ChargeExtras.FirstOrDefaultAsync(extra => extra.Id == extraId, cancellationToken);

    public async Task<IReadOnlyList<OperationsChargeExtra>> ListExtrasAsync(CancellationToken cancellationToken) =>
        await _context.ChargeExtras.AsNoTracking().OrderBy(extra => extra.Name).Take(OperationsChargeLimits.MaxExtras).ToListAsync(cancellationToken);

    public Task<OperationsInsurancePlan?> FindInsurancePlanAsync(OperationsTransportMode mode, CancellationToken cancellationToken) =>
        _context.InsurancePlans.Where(plan => plan.Mode == mode).OrderByDescending(plan => plan.IsActive).ThenBy(plan => plan.Position).FirstOrDefaultAsync(cancellationToken);

    public Task<OperationsInsurancePlan?> GetInsurancePlanAsync(Guid planId, CancellationToken cancellationToken) =>
        _context.InsurancePlans.FirstOrDefaultAsync(plan => plan.Id == planId, cancellationToken);

    public async Task<IReadOnlyList<OperationsInsurancePlan>> ListInsurancePlansAsync(CancellationToken cancellationToken) =>
        await _context.InsurancePlans.AsNoTracking().OrderBy(plan => plan.Mode).ThenBy(plan => plan.Position).ThenBy(plan => plan.Name).ToListAsync(cancellationToken);

    public Task<OperationsQuote?> GetQuoteAsync(Guid quoteId, CancellationToken cancellationToken) =>
        _context.Quotes.FirstOrDefaultAsync(quote => quote.Id == quoteId, cancellationToken);

    public Task<OperationsQuote?> FindQuoteByNumberAsync(string number, CancellationToken cancellationToken) =>
        _context.Quotes.AsNoTracking().FirstOrDefaultAsync(quote => quote.Number == number, cancellationToken);

    public Task<OperationsQuote?> FindQuoteByKeyAsync(string requestKey, CancellationToken cancellationToken) =>
        _context.Quotes.AsNoTracking().FirstOrDefaultAsync(quote => quote.RequestKey == requestKey, cancellationToken);

    public async Task<IReadOnlyList<OperationsQuote>> ListQuotesAsync(Guid? customerId, int take, CancellationToken cancellationToken) =>
        await _context.Quotes.AsNoTracking().Where(quote => customerId == null || quote.CustomerId == customerId)
            .OrderByDescending(quote => quote.CreatedUtc).Take(Math.Clamp(take, 1, 200)).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<OperationsQuote> Items, int TotalCount, Vixly.Operations.Application.Charges.OperationsQuoteCounts Counts)> SearchQuotesAsync(
        Vixly.Operations.Application.Charges.OperationsQuoteFilter filter, DateTimeOffset utcNow, int page, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        IQueryable<OperationsQuote> query = _context.Quotes.AsNoTracking();

        // Typed: the quote's number, or the quotes of the customers it found.
        if (filter.Number is { Length: > 0 } number)
        {
            List<Guid> customers = (filter.CustomerIds ?? Array.Empty<Guid>()).Take(200).ToList();
            query = query.Where(quote => quote.Number.StartsWith(number) || customers.Contains(quote.CustomerId));
        }

        OperationsQuoteStatus open = OperationsQuoteStatus.Open;
        Vixly.Operations.Application.Charges.OperationsQuoteCounts counts = new(
            await query.CountAsync(quote => quote.Status == open && quote.ValidUntilUtc >= utcNow, cancellationToken),
            await query.CountAsync(quote => quote.Status == open && quote.ValidUntilUtc < utcNow, cancellationToken),
            await query.CountAsync(quote => quote.Status == OperationsQuoteStatus.Withdrawn, cancellationToken));

        query = filter.State switch
        {
            Vixly.Operations.Application.Charges.OperationsQuoteState.Open => query.Where(quote => quote.Status == open && quote.ValidUntilUtc >= utcNow),
            Vixly.Operations.Application.Charges.OperationsQuoteState.Expired => query.Where(quote => quote.Status == open && quote.ValidUntilUtc < utcNow),
            Vixly.Operations.Application.Charges.OperationsQuoteState.Withdrawn => query.Where(quote => quote.Status == OperationsQuoteStatus.Withdrawn),
            _ => query
        };

        int size = Math.Clamp(pageSize, 1, 100);
        int total = filter.State switch
        {
            Vixly.Operations.Application.Charges.OperationsQuoteState.Open => counts.Open,
            Vixly.Operations.Application.Charges.OperationsQuoteState.Expired => counts.Expired,
            Vixly.Operations.Application.Charges.OperationsQuoteState.Withdrawn => counts.Withdrawn,
            _ => counts.All
        };
        List<OperationsQuote> items = await query.OrderByDescending(quote => quote.CreatedUtc).ThenByDescending(quote => quote.Number)
            .Skip((Math.Max(page, 1) - 1) * size).Take(size).ToListAsync(cancellationToken);
        return (items, total, counts);
    }
}
