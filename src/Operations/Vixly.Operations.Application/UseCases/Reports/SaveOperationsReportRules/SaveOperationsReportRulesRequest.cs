namespace Vixly.Operations.Application.UseCases.Reports.SaveOperationsReportRules;

// =========================================================
// FILE: SaveOperationsReportRulesRequest.cs
//
// PURPOSE:
// Saves when a dispatch or a parcel counts as on time (Slice 11; owner, 2026-10-22: company settings).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsReportRulesRequest
{
    public int DispatchGraceHours { get; init; }

    // Days after arrival for a parcel of each service to be ready; none: not measured.
    public IReadOnlyDictionary<string, int?> ReadyTargetDays { get; init; } = new Dictionary<string, int?>();
}
