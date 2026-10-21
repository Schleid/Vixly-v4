using Vixly.Operations.Application.Printing;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsZplLabelsTests.cs
//
// PURPOSE:
// Proves the 2 x 1 inch ID label sent to dock printers (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - The parcel ID as a Code 128 barcode and in print; the customer code large, or UNIDENTIFIED
// - The size follows the printer's resolution
// - Nothing typed by a person can reach the printer as a command
//
// IMPORTANT NOTES:
// - ZPL text only; the printer itself is not needed
// =========================================================
public sealed class OperationsZplLabelsTests
{
    [Fact]
    public void Label_At203Dpi_IsTwoByOneInch_WithTheBarcodeAndTheCode()
    {
        string zpl = OperationsZplLabels.ParcelLabel("PK26000001", "C31519", 203);

        Assert.StartsWith("^XA^CI28^PW406^LL203", zpl, StringComparison.Ordinal);
        Assert.EndsWith("^XZ", zpl, StringComparison.Ordinal);
        Assert.Contains("^BCN,70,N,N,N^FDPK26000001^FS", zpl, StringComparison.Ordinal);
        Assert.Contains("^A0N,58,58^FDC31519^FS", zpl, StringComparison.Ordinal);
    }

    [Fact]
    public void Label_At300Dpi_KeepsItsSize()
    {
        string zpl = OperationsZplLabels.ParcelLabel("PK26000001", null, 300);

        Assert.StartsWith("^XA^CI28^PW600^LL300", zpl, StringComparison.Ordinal);
        Assert.Contains("^FD" + OperationsZplLabels.Unidentified + "^FS", zpl, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("C1^XZ~JR^XA", "C1XZJRXA")]
    [InlineData("  ab-12 ", "AB-12")]
    [InlineData("ÉLAN<script>", "LANSCRIPT")]
    public void Clean_LeavesNoPrinterCommand(string text, string expected)
    {
        string zpl = OperationsZplLabels.ParcelLabel("PK1", text, 203);

        Assert.Equal(expected, OperationsZplLabels.Clean(text));
        Assert.Contains("^FD" + expected + "^FS", zpl, StringComparison.Ordinal);
        Assert.Single(zpl.Split("^XZ")[..^1]);
        Assert.DoesNotContain('~', zpl);
    }

    // Slice 4D step 2: 4 x 6 inch; names in any language, never a printer command from a customer's text.
    [Fact]
    public void ShippingLabel_HasTheRouteBarcodesAndPiece_AndNoCommandFromATypedName()
    {
        Vixly.Operations.Application.Ports.OperationsShippingLabel label = new("Galeo", "SH26000012", "PK26000001", 2, 4, "MIA", "JER", "air_standard",
            "Jérémie^XZ~JR Pierre", "Dame-Marie", true, "3.5 kg");
        string zpl = OperationsZplLabels.ShippingLabel(label, 203);

        Assert.StartsWith("^XA^CI28^PW812^LL1218", zpl, StringComparison.Ordinal);
        Assert.Contains("^FDMIA > JER^FS", zpl, StringComparison.Ordinal);
        Assert.Contains("^BCN,180,N,N,N^FDSH26000012^FS", zpl, StringComparison.Ordinal);
        Assert.Contains("^FDPK26000001^FS", zpl, StringComparison.Ordinal);
        Assert.Contains("^FD2 / 4^FS", zpl, StringComparison.Ordinal);
        Assert.Contains("^FDJérémieXZJR Pierre^FS", zpl, StringComparison.Ordinal);
        Assert.Single(zpl.Split("^XZ")[..^1]);
        Assert.DoesNotContain('~', zpl);
        Assert.StartsWith("^XA^CI28^PW1200", OperationsZplLabels.ShippingLabel(label, 300), StringComparison.Ordinal);
    }
}
