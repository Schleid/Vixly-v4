namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsDestinationTerms.cs
//
// PURPOSE:
// What a warehouse offers as a destination: home delivery, its hub, who runs it, the area it delivers to (Slice 4C).
//
// RESPONSIBILITIES:
// - Carry the typed values to OperationsWarehouse.ChangeDestination
//
// IMPORTANT NOTES:
// - Pickup is the warehouse's pickup-point role; checked by the warehouse
// =========================================================
public sealed record OperationsDestinationTerms(
    bool DeliversHome,
    Guid? HubWarehouseId,
    OperationsDestinationOperator OperatedBy,
    string? PartnerName,
    string? PartnerPhone,
    string? PartnerEmail,
    IReadOnlyList<string>? HomeArea);
