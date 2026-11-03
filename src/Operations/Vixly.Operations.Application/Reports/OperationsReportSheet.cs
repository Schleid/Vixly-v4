namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportSheet.cs
//
// PURPOSE:
// A report ready to be written to a file (Slice 11): words already in the reader’s language.
//
// RESPONSIBILITIES:
// - Carry the title, the notes (company, period, warehouse), the headings and the rows (totals last)
//
// IMPORTANT NOTES:
// - Built by the web layer from a report run; the same sheet makes the CSV, the Excel file and the PDF
// =========================================================
public sealed record OperationsReportSheet(string Title, IReadOnlyList<string> Notes, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<OperationsReportCell>> Rows);
