namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsSitePreset.cs
//
// PURPOSE:
// The colour presets a tenant site starts from (Slice 1B).
//
// RESPONSIBILITIES:
// - Name the presets
//
// IMPORTANT NOTES:
// - Stored as int; templates come from the marketplace later (ADR-0017)
// =========================================================
public enum OperationsSitePreset
{
    Ocean = 0,
    Harbour = 1,
    Sunrise = 2,
    Forest = 3,
    Graphite = 4,
    Ruby = 5
}
