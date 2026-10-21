using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsChargeRepository.cs
//
// PURPOSE:
// Stores the company's rate cards, extras, insurance plan and quotes (Slice 4).
//
// RESPONSIBILITIES:
// - Add, find and list them in the current tenant
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1); lists are bounded
// =========================================================
public interface IOperationsChargeRepository
{
    void Add(OperationsRateCard card);

    void Add(OperationsChargeExtra extra);

    void Add(OperationsInsurancePlan plan);

    void Add(OperationsQuote quote);

    void Add(OperationsItemType itemType);

    Task<OperationsItemType?> GetItemTypeAsync(Guid itemTypeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsItemType>> ListItemTypesAsync(CancellationToken cancellationToken);

    // Any price list at all: the company's units are then locked (a price per lb is not a price per kg).
    Task<bool> AnyCardAsync(CancellationToken cancellationToken);

    // Tracked, to change it.
    Task<OperationsRateCard?> GetCardAsync(Guid cardId, CancellationToken cancellationToken);

    Task<OperationsRateCard?> FindCardAsync(string serviceCode, Guid? originWarehouseId, Guid? destinationWarehouseId, Guid? customerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsRateCard>> ListCardsAsync(CancellationToken cancellationToken);

    // In use, the company's standard cards and the customer's own (none when no customer).
    Task<IReadOnlyList<OperationsRateCard>> ListCardsForAsync(Guid? customerId, CancellationToken cancellationToken);

    Task<OperationsChargeExtra?> GetExtraAsync(Guid extraId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsChargeExtra>> ListExtrasAsync(CancellationToken cancellationToken);

    // Slice 4C: one plan per mode; tracked, to change it.
    Task<OperationsInsurancePlan?> FindInsurancePlanAsync(OperationsTransportMode mode, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsInsurancePlan>> ListInsurancePlansAsync(CancellationToken cancellationToken);

    // Tracked, to change it.
    Task<OperationsInsurancePlan?> GetInsurancePlanAsync(Guid planId, CancellationToken cancellationToken);

    Task<OperationsQuote?> GetQuoteAsync(Guid quoteId, CancellationToken cancellationToken);

    Task<OperationsQuote?> FindQuoteByNumberAsync(string number, CancellationToken cancellationToken);

    Task<OperationsQuote?> FindQuoteByKeyAsync(string requestKey, CancellationToken cancellationToken);

    // Newest first; a customer's own, or the company's when no customer.
    Task<IReadOnlyList<OperationsQuote>> ListQuotesAsync(Guid? customerId, int take, CancellationToken cancellationToken);

    // The quotes list (owner, 2026-10-20): newest first, one page, with the count for each state.
    Task<(IReadOnlyList<OperationsQuote> Items, int TotalCount, Vixly.Operations.Application.Charges.OperationsQuoteCounts Counts)> SearchQuotesAsync(
        Vixly.Operations.Application.Charges.OperationsQuoteFilter filter, DateTimeOffset utcNow, int page, int pageSize, CancellationToken cancellationToken);
}
