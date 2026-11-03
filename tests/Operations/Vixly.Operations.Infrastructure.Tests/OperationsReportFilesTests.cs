using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Infrastructure.Documents;
using Vixly.Operations.Infrastructure.Reports;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsReportFilesTests.cs
//
// PURPOSE:
// Proves the report files (Slice 11): a valid Excel workbook and a PDF.
//
// RESPONSIBILITIES:
// - The workbook opens as Office Open XML with the title, headings, text as text and figures as numbers
// - A formula-looking text stays text; the PDF is a PDF
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsReportFilesTests
{
    private static readonly OperationsReportSheet Sheet = new("Sales: 2026/10", new[] { "Report Co" }, new[] { "Item", "Total" }, new[]
    {
        (IReadOnlyList<OperationsReportCell>)new[] { new OperationsReportCell("=SUM(A1:A9)"), new OperationsReportCell("12.50 USD", 12.5m) },
        new[] { new OperationsReportCell("Tom & <Jerry>"), new OperationsReportCell("—") },
        new[] { new OperationsReportCell("bad\uFFFF\uD800 end \uD83D\uDE00"), new OperationsReportCell("x") }
    });

    private static OperationsReportFiles Files() => new(Options.Create(new OperationsDocumentOptions()));

    [Fact]
    public void Workbook_IsOfficeOpenXml_WithNumbersAsNumbers_AndTextAsText()
    {
        using ZipArchive zip = new(new MemoryStream(Files().Xlsx(Sheet)));
        Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument workbook = XDocument.Load(zip.GetEntry("xl/workbook.xml")!.Open());
        Assert.Equal("Sales 2026", workbook.Descendants(main + "sheet").Single().Attribute("name")!.Value[..10]);
        XDocument sheet = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        List<XElement> cells = sheet.Descendants(main + "c").ToList();
        Assert.Contains(cells, cell => cell.Element(main + "v")?.Value == "12.5" && cell.Attribute("t") is null);
        Assert.Contains(cells, cell => cell.Attribute("t")?.Value == "inlineStr" && cell.Descendants(main + "t").Single().Value == "=SUM(A1:A9)");
        Assert.Contains(cells, cell => cell.Descendants(main + "t").SingleOrDefault()?.Value == "Tom & <Jerry>");
        Assert.DoesNotContain(cells, cell => cell.Element(main + "f") is not null);

        // Characters XML cannot carry are left out; a whole emoji stays.
        Assert.Contains(cells, cell => cell.Descendants(main + "t").SingleOrDefault()?.Value == "bad end \uD83D\uDE00");
    }

    [Fact]
    public void Pdf_IsAPdf()
    {
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(Files().Pdf(Sheet)[..4]));
    }
}
