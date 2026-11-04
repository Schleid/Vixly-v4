namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportLabel.cs
//
// PURPOSE:
// What a report row is about (Slice 11).
//
// RESPONSIBILITIES:
// - A period (its first day), a name, or a word to translate
//
// IMPORTANT NOTES:
// - Exactly one is set
// =========================================================
public sealed record OperationsReportLabel(DateOnly? Date = null, string? Text = null, string? TextKey = null);
