using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsCustomerShipment.cs
//
// PURPOSE:
// A shipment as its customer sees it (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the instructions, the origin and destination names, and the counts of announced and arrived packages
//
// IMPORTANT NOTES:
// - Only ever built for the shipment's own customer
// =========================================================
public sealed record OperationsCustomerShipment(
    Guid ShipmentId,
    bool HasInstructions,
    bool IsOpen,
    string? OriginName,
    Guid? BranchId,
    string? BranchName,
    OperationsPostalAddress? Address,
    string? CountryCode,
    string? RecipientName,
    string? RecipientPhone,
    OperationsShipmentOptions Options,
    int Announced,
    int Arrived,
    DateTimeOffset CreatedUtc,
    Guid? DestinationId = null,
    string? ServiceCode = null,
    Guid? OriginId = null,
    int? ParcelCount = null,
    bool Booked = false,
    decimal? BookedTotal = null,
    string? BookedCurrency = null,
    Guid? InsurancePlanId = null)
{
    // Slice 7 step 2 (R-6): who may collect; when it was collected.
    public string? CollectorName { get; init; }

    public string? CollectorPhone { get; init; }

    public DateTimeOffset? PickedUpUtc { get; init; }

    // Slice 7 step 3: when it was delivered at its address.
    public DateTimeOffset? DeliveredUtc { get; init; }

    public bool Collect => Options.HasFlag(OperationsShipmentOptions.FreightCollect);

    public static OperationsCustomerShipment Of(OperationsShipment shipment, string? originName, string? branchName, int announced, int arrived)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        return new(shipment.Id, shipment.HasInstructions, shipment.Status == OperationsShipmentStatus.Open, originName, shipment.DestinationBranchId, branchName, shipment.DeliveryAddress,
            shipment.DestinationCountryCode, shipment.RecipientName, shipment.RecipientPhone, shipment.Options, announced, arrived, shipment.CreatedUtc,
            shipment.DestinationWarehouseId, shipment.ServiceCode, shipment.OriginWarehouseId, shipment.ParcelCount, shipment.IsBooked, shipment.BookedTotal, shipment.BookedCurrencyCode,
            shipment.InsurancePlanId)
        {
            CollectorName = shipment.CollectorName, CollectorPhone = shipment.CollectorPhone, PickedUpUtc = shipment.PickedUpUtc,
            DeliveredUtc = shipment.DeliveredUtc
        };
    }
}
