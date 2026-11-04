using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;

namespace Vixly.Operations.Infrastructure.Reports;

// =========================================================
// FILE: OperationsReportFiles.cs
//
// PURPOSE:
// Writes report sheets as Excel workbooks and PDFs (Slice 11).
//
// RESPONSIBILITIES:
// - Excel: a minimal Office Open XML workbook (one sheet, inline text, numbers as numbers, bold headings)
// - PDF: the title, the notes and the table, landscape, repeated headings
//
// IMPORTANT NOTES:
// - Values only: a text that looks like a formula stays text
// - No third-party spreadsheet library: the format is written by hand (System.IO.Compression)
// =========================================================
internal sealed class OperationsReportFiles : IOperationsReportFiles
{
    // The QuestPDF licence type from configuration, as for every other document.
    public OperationsReportFiles(Microsoft.Extensions.Options.IOptions<Vixly.Operations.Infrastructure.Documents.OperationsDocumentOptions> options)
    {
        string license = options?.Value.QuestPdfLicense ?? string.Empty;
        QuestPDF.Settings.License = Enum.TryParse(license, ignoreCase: true, out LicenseType type) ? type : LicenseType.Community;
    }

    public byte[] Xlsx(OperationsReportSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        using MemoryStream stream = new();

        using (ZipArchive zip = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Entry(zip, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>
                """);
            Entry(zip, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            Entry(zip, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>
                """);
            Entry(zip, "xl/workbook.xml", $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="{Xml(SheetName(sheet.Title))}" sheetId="1" r:id="rId1"/></sheets></workbook>
                """);
            Entry(zip, "xl/styles.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border/></borders><cellStyleXfs count="1"><xf/></cellStyleXfs><cellXfs count="2"><xf fontId="0"/><xf fontId="1" applyFont="1"/></cellXfs></styleSheet>
                """);
            StringBuilder rows = new();
            int number = 0;

            void Row(IEnumerable<OperationsReportCell> cells, bool bold)
            {
                number++;
                rows.Append("<row r=\"").Append(number).Append("\">");
                int column = 0;

                foreach (OperationsReportCell cell in cells)
                {
                    string reference = Column(column++) + number.ToString(CultureInfo.InvariantCulture);
                    string style = bold ? " s=\"1\"" : string.Empty;
                    rows.Append(cell.Number is { } value
                        ? $"<c r=\"{reference}\"{style}><v>{value.ToString(CultureInfo.InvariantCulture)}</v></c>"
                        : $"<c r=\"{reference}\"{style} t=\"inlineStr\"><is><t xml:space=\"preserve\">{Xml(cell.Text)}</t></is></c>");
                }

                rows.Append("</row>");
            }

            Row(new[] { new OperationsReportCell(sheet.Title) }, true);

            foreach (string note in sheet.Notes)
            {
                Row(new[] { new OperationsReportCell(note) }, false);
            }

            Row(Array.Empty<OperationsReportCell>(), false);
            Row(sheet.Headers.Select(header => new OperationsReportCell(header)), true);

            foreach (IReadOnlyList<OperationsReportCell> row in sheet.Rows)
            {
                Row(row, false);
            }

            Entry(zip, "xl/worksheets/sheet1.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>"
                + rows + "</sheetData></worksheet>");
        }

        return stream.ToArray();
    }

    public byte[] Pdf(OperationsReportSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        return Document.Create(pdf => pdf.Page(page =>
        {
            page.Size(PageSizes.Letter.Landscape());
            page.Margin(28);
            page.DefaultTextStyle(style => style.FontSize(8.5f).FontColor("#1F2933"));
            page.Header().Column(column =>
            {
                column.Item().Text(sheet.Title).FontSize(14).SemiBold();

                foreach (string note in sheet.Notes)
                {
                    column.Item().Text(note).FontColor("#52606D");
                }
            });
            page.Content().PaddingVertical(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);

                    for (int column = 1; column < sheet.Headers.Count; column++)
                    {
                        columns.RelativeColumn(2);
                    }
                });
                table.Header(header =>
                {
                    for (int column = 0; column < sheet.Headers.Count; column++)
                    {
                        IContainer cell = header.Cell().BorderBottom(1).PaddingBottom(3);
                        (column == 0 ? cell : cell.AlignRight()).Text(sheet.Headers[column]).SemiBold();
                    }
                });

                foreach (IReadOnlyList<OperationsReportCell> row in sheet.Rows)
                {
                    for (int column = 0; column < sheet.Headers.Count; column++)
                    {
                        IContainer cell = table.Cell().BorderBottom(0.5f).BorderColor("#D9E2EC").PaddingVertical(3);
                        string text = column < row.Count ? row[column].Text : string.Empty;
                        (column == 0 ? cell : cell.AlignRight()).Text(text);
                    }
                }
            });
            page.Footer().AlignRight().Text(text =>
            {
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        })).GeneratePdf();
    }

    private static void Entry(ZipArchive zip, string name, string content)
    {
        using StreamWriter writer = new(zip.CreateEntry(name, CompressionLevel.Optimal).Open(), new UTF8Encoding(false));
        writer.Write(content.Trim());
    }

    private static string Column(int index)
    {
        string name = string.Empty;

        for (index++; index > 0; index = (index - 1) / 26)
        {
            name = (char)('A' + ((index - 1) % 26)) + name;
        }

        return name;
    }

    // Excel forbids : \ / ? * [ ] in a sheet name, and an apostrophe at either end; 31 characters, never half a character pair.
    private static string SheetName(string title)
    {
        string clean = new(Valid(title).Where(character => character is not (':' or '\\' or '/' or '?' or '*' or '[' or ']')).ToArray());
        clean = clean.Trim('\'', ' ');

        if (clean.Length > 31)
        {
            clean = clean[..(char.IsHighSurrogate(clean[30]) ? 30 : 31)];
        }

        return clean.Length == 0 ? "Report" : clean;
    }

    // Text escaped for XML, without the characters XML cannot carry (review: lone surrogates, U+FFFE, U+FFFF, control characters).
    private static string Xml(string value) => SecurityElement.Escape(Valid(value)) ?? string.Empty;

    private static string Valid(string? value)
    {
        StringBuilder kept = new();
        string text = value ?? string.Empty;

        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];

            if (char.IsHighSurrogate(character) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
            {
                kept.Append(character).Append(text[++index]);
            }
            else if (character is '\t' or '\n' or '\r' || (character >= ' ' && character < '\uD800') || (character >= '' && character <= '�'))
            {
                kept.Append(character);
            }
        }

        return kept.ToString();
    }
}
