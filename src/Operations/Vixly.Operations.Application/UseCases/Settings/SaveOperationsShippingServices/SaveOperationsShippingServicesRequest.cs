using Vixly.Operations.Application.Settings;

namespace Vixly.Operations.Application.UseCases.Settings.SaveOperationsShippingServices;

// =========================================================
// FILE: SaveOperationsShippingServicesRequest.cs
//
// PURPOSE:
// Saves which shipping services the tenant offers, and how (Slice 1E, D-O15).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsShippingServicesRequest
{
    public IReadOnlyList<OperationsShippingServiceRow> Services { get; init; } = Array.Empty<OperationsShippingServiceRow>();
}
