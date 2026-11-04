namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsTransportMode.cs
//
// PURPOSE:
// How a service travels: by air, by sea or by road (Slice 4C).
//
// RESPONSIBILITIES:
// - Name the modes; each has its own prices, extras and insurance
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsTransportMode
{
    Air = 1,

    Ocean = 2,

    Ground = 3
}
