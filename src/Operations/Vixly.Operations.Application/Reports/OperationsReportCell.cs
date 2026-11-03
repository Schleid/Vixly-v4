namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportCell.cs
//
// PURPOSE:
// One cell of a report file (Slice 11).
//
// RESPONSIBILITIES:
// - Carry the text as shown and, for a figure, its number
//
// IMPORTANT NOTES:
// - Spreadsheets get the number; CSV and PDF the text
// =========================================================
public sealed record OperationsReportCell(string Text, decimal? Number = null);
