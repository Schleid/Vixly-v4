namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsTransportModes.cs
//
// PURPOSE:
// The transport mode of a shipping service code (Slice 4C).
//
// RESPONSIBILITIES:
// - Air services fly by weight; ocean services sail by size and item; ground goes by road
//
// IMPORTANT NOTES:
// - The service codes come from the platform catalog: air_*, ocean_*, ground
// =========================================================
public static class OperationsTransportModes
{
    public static OperationsTransportMode Of(string? serviceCode) =>
        serviceCode is null ? OperationsTransportMode.Air
        : serviceCode.StartsWith("ocean", StringComparison.Ordinal) ? OperationsTransportMode.Ocean
        : serviceCode.StartsWith("ground", StringComparison.Ordinal) ? OperationsTransportMode.Ground
        : OperationsTransportMode.Air;
}
