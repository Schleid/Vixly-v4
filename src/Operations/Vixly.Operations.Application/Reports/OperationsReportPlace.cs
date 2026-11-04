namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportPlace.cs
//
// PURPOSE:
// A warehouse a report covers (Slice 11).
//
// RESPONSIBILITIES:
// - Carry its id, code, name and time zone
//
// IMPORTANT NOTES:
// - Days are counted in each warehouse’s own time zone (owner, 2026-10-22)
// =========================================================
public sealed record OperationsReportPlace(Guid Id, string Code, string Name, string TimeZone);
