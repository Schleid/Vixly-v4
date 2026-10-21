using System.Globalization;
using System.Text;

namespace Vixly.Operations.Application.Printing;

// =========================================================
// FILE: OperationsZplLabels.cs
//
// PURPOSE:
// Writes labels in ZPL, the language of thermal label printers (Slice 3B).
//
// RESPONSIBILITIES:
// - Write the 2 x 1 inch parcel ID label at 203 or 300 dpi
// - Write the 4 x 6 inch shipping label (Slice 4D step 2)
//
// IMPORTANT NOTES:
// - The printer draws the Code 128 barcode itself (^BC)
// - Barcodes and codes keep only letters, digits, spaces and dashes; names keep letters (any language), digits and plain punctuation,
//   never the ^ and ~ that start printer commands, so no field can carry printer commands
// =========================================================
public static class OperationsZplLabels
{
    public const string Unidentified = "UNIDENTIFIED";

    // 2 x 1 inch: the parcel ID as a barcode and in print, the customer code large for sorting by eye.
    public static string ParcelLabel(string parcelNumber, string? customerCode, int dpi)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parcelNumber);
        double scale = dpi >= 300 ? 300d / 203d : 1d;
        string number = Clean(parcelNumber);
        string code = string.IsNullOrWhiteSpace(customerCode) ? Unidentified : Clean(customerCode);
        int codeHeight = code.Length > 8 ? 40 : 58;
        StringBuilder zpl = new();
        zpl.Append("^XA^CI28");
        zpl.Append(CultureInfo.InvariantCulture, $"^PW{Dots(406, scale)}^LL{Dots(203, scale)}^LH0,0");
        zpl.Append(CultureInfo.InvariantCulture, $"^FO{Dots(24, scale)},{Dots(12, scale)}^BY{(scale > 1 ? 3 : 2)},3,{Dots(70, scale)}^BCN,{Dots(70, scale)},N,N,N^FD{number}^FS");
        zpl.Append(CultureInfo.InvariantCulture, $"^FO{Dots(24, scale)},{Dots(88, scale)}^A0N,{Dots(24, scale)},{Dots(24, scale)}^FD{number}^FS");
        zpl.Append(CultureInfo.InvariantCulture, $"^FO{Dots(24, scale)},{Dots(122, scale)}^A0N,{Dots(codeHeight, scale)},{Dots(codeHeight, scale)}^FD{code}^FS");
        zpl.Append("^XZ");
        return zpl.ToString();
    }

    // 4 x 6 inch (812 x 1218 dots at 203 dpi): the route large, the shipment and parcel barcodes, the recipient, the service and the weight.
    public static string ShippingLabel(Vixly.Operations.Application.Ports.OperationsShippingLabel label, int dpi)
    {
        ArgumentNullException.ThrowIfNull(label);
        double scale = dpi >= 300 ? 300d / 203d : 1d;
        int bar = scale > 1 ? 4 : 3;
        string Field(int x, int y, int height, string text) =>
            string.Create(CultureInfo.InvariantCulture, $"^FO{Dots(x, scale)},{Dots(y, scale)}^A0N,{Dots(height, scale)},{Dots(height, scale)}^FD{text}^FS");
        StringBuilder zpl = new();
        zpl.Append("^XA^CI28");
        zpl.Append(CultureInfo.InvariantCulture, $"^PW{Dots(812, scale)}^LL{Dots(1218, scale)}^LH0,0");
        zpl.Append(Field(40, 30, 34, Words(label.CompanyName)));
        zpl.Append(Field(40, 90, 120, Clean(label.OriginCode) + " > " + Clean(label.DestinationCode)));
        zpl.Append(CultureInfo.InvariantCulture, $"^FO{Dots(40, scale)},{Dots(240, scale)}^GB{Dots(732, scale)},{Dots(4, scale)},{Dots(4, scale)}^FS");
        zpl.Append(Field(40, 270, 44, Words(label.RecipientName)));
        zpl.Append(Field(40, 330, 44, Words(label.RecipientCity) + " · " + (label.Pickup ? "PICKUP" : "HOME")));
        zpl.Append(Field(40, 400, 36, Clean(label.ServiceCode?.Replace('_', ' ') ?? "-") + "   " + Words(label.Weight)));
        zpl.Append(Field(560, 400, 44, string.Create(CultureInfo.InvariantCulture, $"{label.Piece} / {label.Pieces}")));
        zpl.Append(CultureInfo.InvariantCulture, $"^FO{Dots(40, scale)},{Dots(480, scale)}^BY{bar},3,{Dots(180, scale)}^BCN,{Dots(180, scale)},N,N,N^FD{Clean(label.ShipmentNumber)}^FS");
        zpl.Append(Field(40, 680, 40, Clean(label.ShipmentNumber)));
        zpl.Append(CultureInfo.InvariantCulture, $"^FO{Dots(40, scale)},{Dots(780, scale)}^BY{bar},3,{Dots(140, scale)}^BCN,{Dots(140, scale)},N,N,N^FD{Clean(label.ParcelNumber)}^FS");
        zpl.Append(Field(40, 940, 36, Clean(label.ParcelNumber)));
        zpl.Append("^XZ");
        return zpl.ToString();
    }

    // A name or a city: letters of any language, digits and plain punctuation; never a printer command.
    public static string Words(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new string(text.Where(character => char.IsLetterOrDigit(character) || " .,'-/()&#".Contains(character, StringComparison.Ordinal)).Take(40).ToArray()).Trim();
    }

    public static string Clean(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new string(text.ToUpperInvariant().Where(character => char.IsAsciiLetterOrDigit(character) || character is ' ' or '-').Take(24).ToArray()).Trim();
    }

    private static int Dots(int at203, double scale) => (int)Math.Round(at203 * scale, MidpointRounding.AwayFromZero);
}
