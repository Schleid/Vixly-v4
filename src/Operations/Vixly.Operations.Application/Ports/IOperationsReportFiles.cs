using Vixly.Operations.Application.Reports;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsReportFiles.cs
//
// PURPOSE:
// Writes a report sheet as an Excel workbook or a PDF (Slice 11).
//
// RESPONSIBILITIES:
// - One sheet, headings bold, figures as numbers (Excel); a landscape table (PDF)
//
// IMPORTANT NOTES:
// - No macros, no formulas: values only
// =========================================================
public interface IOperationsReportFiles
{
    byte[] Xlsx(OperationsReportSheet sheet);

    byte[] Pdf(OperationsReportSheet sheet);
}
