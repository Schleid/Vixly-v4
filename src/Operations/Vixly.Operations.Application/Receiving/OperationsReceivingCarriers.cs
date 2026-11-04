namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsReceivingCarriers.cs
//
// PURPOSE:
// The carriers a clerk picks when a delivery arrives (Slice 3B).
//
// RESPONSIBILITIES:
// - List the usual carriers for one-tap choice
//
// IMPORTANT NOTES:
// - Any other carrier may be typed; the list only saves time
// =========================================================
public static class OperationsReceivingCarriers
{
    public static IReadOnlyList<string> Common { get; } = new[] { "Amazon", "UPS", "FedEx", "USPS", "DHL" };
}
