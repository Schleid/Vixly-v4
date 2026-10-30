namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsLocationDetails.cs
//
// PURPOSE:
// The editable details shared by branches and warehouses.
//
// RESPONSIBILITIES:
// - Group name, address, contact and hours
//
// IMPORTANT NOTES:
// - Validated by the entity
// =========================================================
public sealed record OperationsLocationDetails(
    string Code,
    string Name,
    OperationsPostalAddress Address,
    string? Phone,
    string? Email,
    string? OpeningHours,
    bool ShowOnSite);
