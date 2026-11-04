using System.Text;

namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportCsv.cs
//
// PURPOSE:
// Writes a report sheet as CSV (Slice 11).
//
// RESPONSIBILITIES:
// - UTF-8 with its mark, comma-separated, one line per row; figures as plain numbers
//
// IMPORTANT NOTES:
// - A cell starting with =, +, -, @, a tab, a return or a new line is guarded (formula injection)
// =========================================================
public static class OperationsReportCsv
{
    public static byte[] Write(OperationsReportSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        StringBuilder csv = new();
        csv.Append(string.Join(',', sheet.Headers.Select(Cell))).Append("\r\n");

        foreach (IReadOnlyList<OperationsReportCell> row in sheet.Rows)
        {
            csv.Append(string.Join(',', row.Select(cell => cell.Number is { } number ? number.ToString(System.Globalization.CultureInfo.InvariantCulture) : Cell(cell.Text)))).Append("\r\n");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public static string Cell(string value) =>
        "\"" + (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' or '\n' ? "'" + value : value).Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
