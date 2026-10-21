using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsDispatchShipment.cs
//
// PURPOSE:
// One customer shipment on a closed dispatch (Slice 6B step 2, D-6 and D-7): its house waybill and its share of the cost.
//
// RESPONSIBILITIES:
// - Hold the house number (HAWB for air, house bill of lading for ocean), pieces, weights and declared value as manifested
// - Hold its share of the carrier's cost
//
// IMPORTANT NOTES:
// - Written when the dispatch closes; removed if the dispatch is cancelled
// - One per shipment per dispatch
// =========================================================
public sealed class OperationsDispatchShipment : OperationsEntity
{
    private OperationsDispatchShipment()
    {
        HouseNumber = string.Empty;
        CurrencyCode = string.Empty;
    }

    private OperationsDispatchShipment(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        HouseNumber = string.Empty;
        CurrencyCode = string.Empty;
    }

    public Guid DispatchId { get; private set; }

    public Guid ShipmentId { get; private set; }

    public string HouseNumber { get; private set; }

    public int Pieces { get; private set; }

    public decimal WeightKg { get; private set; }

    public decimal ChargeableKg { get; private set; }

    public decimal DeclaredValue { get; private set; }

    public string CurrencyCode { get; private set; }

    // Its share of the dispatch's cost, by chargeable weight (D-7); none until the cost is known.
    public decimal? CostShare { get; private set; }

    // The house number: the dispatch's number and the shipment's place on it (DSP26000001-001), unique in the company.
    public static string House(string dispatchNumber, int place) =>
        dispatchNumber + "-" + place.ToString("000", System.Globalization.CultureInfo.InvariantCulture);

    public static OperationsDispatchShipment Create(Guid tenantId, Guid dispatchId, Guid shipmentId, string houseNumber, int pieces, decimal weightKg, decimal chargeableKg, decimal declaredValue,
        string? currencyCode, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(houseNumber);

        if (pieces < 1 || weightKg < 0m || chargeableKg < 0m || declaredValue < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(pieces));
        }

        return new OperationsDispatchShipment(tenantId, utcNow)
        {
            DispatchId = dispatchId,
            ShipmentId = shipmentId,
            HouseNumber = houseNumber,
            Pieces = pieces,
            WeightKg = weightKg,
            ChargeableKg = chargeableKg,
            DeclaredValue = declaredValue,
            CurrencyCode = currencyCode ?? string.Empty
        };
    }

    public void Share(decimal? amount, DateTimeOffset utcNow)
    {
        CostShare = amount;
        MarkAsModified(utcNow);
    }
}
