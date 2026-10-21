using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeChargeStore.cs
//
// PURPOSE:
// An in-memory store of price lists, extras, insurance, quotes and item types (Slice 4B).
//
// RESPONSIBILITIES:
// - Hold what a test adds
//
// IMPORTANT NOTES:
// - Tests only
// =========================================================
internal sealed class FakeChargeStore : IOperationsChargeRepository
{
    public List<OperationsRateCard> Cards { get; } = new();

    public List<OperationsChargeExtra> Extras { get; } = new();

    public List<OperationsQuote> Quotes { get; } = new();

    public List<OperationsItemType> ItemTypes { get; } = new();

    public OperationsInsurancePlan? Plan { get; private set; }

    public void Add(OperationsRateCard card) => Cards.Add(card);

    public void Add(OperationsChargeExtra extra) => Extras.Add(extra);

    public void Add(OperationsInsurancePlan plan) => Plan = plan;

    public void Add(OperationsQuote quote) => Quotes.Add(quote);

    public void Add(OperationsItemType itemType) => ItemTypes.Add(itemType);

    public Task<OperationsItemType?> GetItemTypeAsync(Guid itemTypeId, CancellationToken cancellationToken) => Task.FromResult(ItemTypes.FirstOrDefault(type => type.Id == itemTypeId));

    public Task<IReadOnlyList<OperationsItemType>> ListItemTypesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OperationsItemType>>(ItemTypes.ToList());

    public Task<bool> AnyCardAsync(CancellationToken cancellationToken) => Task.FromResult(Cards.Count > 0);

    public Task<OperationsRateCard?> GetCardAsync(Guid cardId, CancellationToken cancellationToken) => Task.FromResult(Cards.FirstOrDefault(card => card.Id == cardId));

    public Task<OperationsRateCard?> FindCardAsync(string serviceCode, Guid? originWarehouseId, Guid? destinationWarehouseId, Guid? customerId, CancellationToken cancellationToken) =>
        Task.FromResult<OperationsRateCard?>(null);

    public Task<IReadOnlyList<OperationsRateCard>> ListCardsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OperationsRateCard>>(Cards.ToList());

    public Task<IReadOnlyList<OperationsRateCard>> ListCardsForAsync(Guid? customerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsRateCard>>(Cards.Where(card => card.IsActive && (card.CustomerId is null || card.CustomerId == customerId)).ToList());

    public Task<OperationsChargeExtra?> GetExtraAsync(Guid extraId, CancellationToken cancellationToken) => Task.FromResult(Extras.FirstOrDefault(extra => extra.Id == extraId));

    public Task<IReadOnlyList<OperationsChargeExtra>> ListExtrasAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OperationsChargeExtra>>(Extras.ToList());

    public Task<OperationsInsurancePlan?> FindInsurancePlanAsync(OperationsTransportMode mode, CancellationToken cancellationToken) => Task.FromResult(Plan?.Mode == mode ? Plan : null);

    public Task<IReadOnlyList<OperationsInsurancePlan>> ListInsurancePlansAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsInsurancePlan>>(Plan is null ? Array.Empty<OperationsInsurancePlan>() : new[] { Plan });

    public Task<OperationsInsurancePlan?> GetInsurancePlanAsync(Guid planId, CancellationToken cancellationToken) => Task.FromResult(Plan?.Id == planId ? Plan : null);

    public Task<OperationsQuote?> GetQuoteAsync(Guid quoteId, CancellationToken cancellationToken) => Task.FromResult(Quotes.FirstOrDefault(quote => quote.Id == quoteId));

    public Task<OperationsQuote?> FindQuoteByNumberAsync(string number, CancellationToken cancellationToken) => Task.FromResult(Quotes.FirstOrDefault(quote => quote.Number == number));

    public Task<OperationsQuote?> FindQuoteByKeyAsync(string requestKey, CancellationToken cancellationToken) => Task.FromResult(Quotes.FirstOrDefault(quote => quote.RequestKey == requestKey));

    public Task<IReadOnlyList<OperationsQuote>> ListQuotesAsync(Guid? customerId, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsQuote>>(Quotes.Where(quote => customerId is null || quote.CustomerId == customerId).Take(take).ToList());

    public Task<(IReadOnlyList<OperationsQuote> Items, int TotalCount, Vixly.Operations.Application.Charges.OperationsQuoteCounts Counts)> SearchQuotesAsync(
        Vixly.Operations.Application.Charges.OperationsQuoteFilter filter, DateTimeOffset utcNow, int page, int pageSize, CancellationToken cancellationToken)
    {
        List<OperationsQuote> all = Quotes.OrderByDescending(quote => quote.CreatedUtc).ToList();
        Vixly.Operations.Application.Charges.OperationsQuoteCounts counts = new(all.Count(quote => quote.IsValid(utcNow)),
            all.Count(quote => quote.Status == OperationsQuoteStatus.Open && !quote.IsValid(utcNow)), all.Count(quote => quote.Status == OperationsQuoteStatus.Withdrawn));
        return Task.FromResult<(IReadOnlyList<OperationsQuote>, int, Vixly.Operations.Application.Charges.OperationsQuoteCounts)>(
            (all.Skip((page - 1) * pageSize).Take(pageSize).ToList(), all.Count, counts));
    }
}
