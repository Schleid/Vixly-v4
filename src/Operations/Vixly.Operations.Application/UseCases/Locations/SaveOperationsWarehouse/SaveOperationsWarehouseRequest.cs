using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.Locations.SaveOperationsWarehouse;

// =========================================================
// FILE: SaveOperationsWarehouseRequest.cs
//
// PURPOSE:
// Creates or changes a warehouse (Slice 1B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsWarehouseRequest
{
    public Guid? WarehouseId { get; init; }

    public Guid BranchId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Line1 { get; init; } = string.Empty;

    public string? Line2 { get; init; }

    public string City { get; init; } = string.Empty;

    public string? Region { get; init; }

    public string? PostalCode { get; init; }

    public string CountryCode { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? OpeningHours { get; init; }

    public bool ShowOnSite { get; init; }

    public OperationsLocationStatus Status { get; init; }

    public OperationsWarehouseRoles Roles { get; init; }

    public bool AcceptingDeliveries { get; init; }

    // Slice 4B: the location's IANA time zone (empty: the company's) and its manager, a staff member.
    public string? TimeZone { get; init; }

    public Guid? ManagerUserId { get; init; }

    // Slice 4C: as a destination: home delivery in its area, the hub that serves it, who runs it.
    public bool DeliversHome { get; init; }

    // Slice 7 (R-3): customs is cleared here: parcels wait for clearance before they are ready.
    public bool ClearsCustoms { get; init; }

    public IReadOnlyList<string>? HomeArea { get; init; }

    public Guid? HubWarehouseId { get; init; }

    public OperationsDestinationOperator OperatedBy { get; init; } = OperationsDestinationOperator.Company;

    public string? PartnerName { get; init; }

    public string? PartnerPhone { get; init; }

    public string? PartnerEmail { get; init; }
}
