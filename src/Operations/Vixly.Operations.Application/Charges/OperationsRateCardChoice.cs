using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsRateCardChoice.cs
//
// PURPOSE:
// Picks the price list that prices a shipment on its route (Slice 4B; routes, Slice 4C design section 5).
//
// RESPONSIBILITIES:
// - The customer's lists from the most specific route to any route, then the standard lists the same way
//
// IMPORTANT NOTES:
// - Lists out of use never count; another customer's never does
// =========================================================
public static class OperationsRateCardChoice
{
    public static OperationsRateCard? Pick(IEnumerable<OperationsRateCard> cards, string serviceCode, Guid? originId, Guid? destinationId, Guid? customerId, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(cards);
        List<OperationsRateCard> usable = cards.Where(card => card.IsActive && card.ServiceCode == serviceCode && card.VersionAt(at) is not null).ToList();

        return (customerId is { } customer ? OnRoute(usable, originId, destinationId, customer) : null) ?? OnRoute(usable, originId, destinationId, null);
    }

    // The most specific route first: both ends, any origin to the destination, the origin to any destination, any route.
    private static OperationsRateCard? OnRoute(List<OperationsRateCard> cards, Guid? originId, Guid? destinationId, Guid? customerId) =>
        (originId is not null && destinationId is not null ? Find(cards, originId, destinationId, customerId) : null)
        ?? (destinationId is not null ? Find(cards, null, destinationId, customerId) : null)
        ?? (originId is not null ? Find(cards, originId, null, customerId) : null)
        ?? Find(cards, null, null, customerId);

    private static OperationsRateCard? Find(List<OperationsRateCard> cards, Guid? originId, Guid? destinationId, Guid? customerId) =>
        cards.FirstOrDefault(card => card.CustomerId == customerId && card.OriginWarehouseId == originId && card.DestinationWarehouseId == destinationId);
}
