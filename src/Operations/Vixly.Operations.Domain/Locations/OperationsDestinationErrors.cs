using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsDestinationErrors.cs
//
// PURPOSE:
// Errors of destination warehouses, time zones and managers (Slice 4B and 4C).
//
// RESPONSIBILITIES:
// - Name each refusal
//
// IMPORTANT NOTES:
// - Texts in OperationsText
// =========================================================
public static class OperationsDestinationErrors
{
    public static Error NotFound { get; } = Error.Create("ops.destinations.not_found");

    public static Error AreaInvalid { get; } = Error.Create("ops.destinations.area_invalid");

    public static Error HubInvalid { get; } = Error.Create("ops.destinations.hub_invalid");

    public static Error PartnerInvalid { get; } = Error.Create("ops.destinations.partner_invalid");

    public static Error TimeZoneInvalid { get; } = Error.Create("ops.destinations.time_zone_invalid");

    public static Error ManagerInvalid { get; } = Error.Create("ops.destinations.manager_invalid");
}
