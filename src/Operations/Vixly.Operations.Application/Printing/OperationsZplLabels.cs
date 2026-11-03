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
// - Write the 4 x 6 inch shipping label (Slice 4D step 2; redesigned with the service's label code, owner 2026-10-31)
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

    // 4 x 6 inch (812 x 1218 dots at 203 dpi), owner 2026-10-31: the logo and ship date; the route beside the service's label code, large, in a box
    // that differs by mode (air solid, ocean striped, ground outlined; express adds a band on top); the recipient with pickup or home; the tracking
    // barcode; weight, flags and piece; the parcel barcode with its number; the shipment number in print at the foot. No customer number, no contents.
    public static string ShippingLabel(Vixly.Operations.Application.Ports.OperationsShippingLabel label, int dpi)
    {
        ArgumentNullException.ThrowIfNull(label);
        double scale = dpi >= 300 ? 300d / 203d : 1d;
        int bar = scale > 1 ? 4 : 3;
        int top = label.Express ? 50 : 0;
        StringBuilder zpl = new();

        // Text at (x, y), "height" dots high; with a width, kept to one line and aligned (L, C or R); reversed: white on a black box.
        void Text(int x, int y, int height, string text, int width = 0, char align = 'L', bool reversed = false)
        {
            zpl.Append(CultureInfo.InvariantCulture, $"^FO{Dots(x, scale)},{Dots(y + top, scale)}^A0N,{Dots(height, scale)},{Dots(height, scale)}");

            if (width > 0)
            {
                zpl.Append(CultureInfo.InvariantCulture, $"^FB{Dots(width, scale)},1,0,{align},0");
            }

            zpl.Append(reversed ? "^FR" : string.Empty).Append("^FD").Append(text).Append("^FS");
        }

        void Box(int x, int y, int width, int height, int thickness) =>
            zpl.Append(CultureInfo.InvariantCulture, $"^FO{Dots(x, scale)},{Dots(y + top, scale)}^GB{Dots(width, scale)},{Dots(height, scale)},{Dots(thickness, scale)}^FS");

        void Rule(int y) => Box(0, y, 812, 6, 6);

        void Barcode(int y, int height, string value) =>
            zpl.Append(CultureInfo.InvariantCulture, $"^FO{Dots(30, scale)},{Dots(y + top, scale)}^BY{bar},3,{Dots(height, scale)}^BCN,{Dots(height, scale)},N,N,N^FD{Clean(value)}^FS");

        // Code 128 (code set B) is 11 modules per character plus 35 for start, check and stop: the widest bar that fits 752 dots, centred.
        void WideBarcode(int y, int height, string value)
        {
            string data = Clean(value);
            int modules = (11 * data.Length) + 35;
            int room = Dots(752, scale);
            int module = Math.Clamp(room / modules, 2, 10);
            int left = (Dots(812, scale) - (module * modules)) / 2;
            zpl.Append(CultureInfo.InvariantCulture, $"^FO{left},{Dots(y + top, scale)}^BY{module},3,{Dots(height, scale)}^BCN,{Dots(height, scale)},N,N,N^FD{data}^FS");
        }

        zpl.Append("^XA^CI28");
        zpl.Append(CultureInfo.InvariantCulture, $"^PW{Dots(812, scale)}^LL{Dots(1218, scale)}^LH0,0");

        if (label.Express)
        {
            zpl.Append(CultureInfo.InvariantCulture, $"^FO0,0^GB{Dots(812, scale)},{Dots(50, scale)},{Dots(50, scale)}^FS");
            zpl.Append(CultureInfo.InvariantCulture, $"^FO0,{Dots(8, scale)}^A0N,{Dots(36, scale)},{Dots(36, scale)}^FB{Dots(812, scale)},1,0,C,0^FR^FDE X P R E S S^FS");
        }

        // Owner, 2026-10-28: the company's black and white logo when it has one, else its name.
        if (OperationsZplLogo.Graphic(label.Logo, Dots(70, scale), Dots(480, scale)) is { } logo)
        {
            zpl.Append(CultureInfo.InvariantCulture, $"^FO{Dots(30, scale)},{Dots(18 + top, scale)}{logo}^FS");
        }
        else
        {
            Text(30, 34, 36, Words(label.CompanyName), 480);
        }

        Text(520, 26, 24, label.ShipDate is { } date ? "Ship date " + Clean(date) : string.Empty, 262, 'R');
        Text(520, 58, 24, Words(label.OriginName ?? label.OriginCode), 262, 'R');
        Rule(106);

        // The route and the service: the code box on the right, 282 x 256 dots.
        Text(30, 150, 96, Clean(label.OriginCode) + " > " + Clean(label.DestinationCode), 490);
        Text(30, 270, 32, Words(label.ServiceName ?? string.Empty), 490);
        ServiceBox(zpl, label, scale, top, 530, 112, 282, 256);
        Rule(368);

        Text(30, 382, 22, "TO");
        Text(30, 410, 48, Words(label.RecipientName).ToUpperInvariant(), 752);
        Text(30, 470, 42, Words(label.RecipientCity), 540);

        if (label.Pickup)
        {
            Box(600, 464, 182, 52, 52);
            Text(600, 476, 32, "PICKUP", 182, 'C', reversed: true);
        }
        else
        {
            Box(600, 464, 182, 52, 5);
            Text(600, 476, 32, "HOME", 182, 'C');
        }

        Rule(570);

        // Owner, 2026-10-28: the tracking number is the main barcode; owner, 2026-10-31: the shipment number in print only, never as a barcode.
        // Owner, 2026-11-01: the tracking barcode fills the row, its number centred under it.
        WideBarcode(592, 150, label.TrackingNumber ?? label.ShipmentNumber);
        Text(30, 754, 40, Clean(label.TrackingNumber is { } tracking ? Vixly.Operations.Domain.Shipments.OperationsTrackingNumbers.Format(tracking) : label.ShipmentNumber), 752, 'C');

        Rule(810);
        Text(30, 828, 24, "Weight");
        Text(30, 860, 44, Words(label.Weight), 260);
        string flags = string.Join("   ", new[] { label.Insured ? "INSURED" : null, label.Consolidated ? "CONSOL" : null }.Where(flag => flag is not null));
        Text(260, 842, 30, flags, 292, 'C');
        Text(560, 828, 24, "Piece", 222, 'R');
        Text(560, 856, 60, string.Create(CultureInfo.InvariantCulture, $"{label.Piece} / {label.Pieces}"), 222, 'R');
        Rule(930);

        Barcode(952, 110, label.ParcelNumber);
        Text(30, 1074, 36, Clean(label.ParcelNumber), 400);

        // Owner, 2026-10-31: the shipment number at the foot of the label, in print only.
        Text(300, 1124, 34, "Shipment " + Clean(label.ShipmentNumber), 482, 'R');
        zpl.Append("^XZ");
        return zpl.ToString();
    }

    // The service's label code, large, in a box that differs by mode so a worker sorts by eye: air solid, ocean striped, ground outlined.
    private static void ServiceBox(StringBuilder zpl, Vixly.Operations.Application.Ports.OperationsShippingLabel label, double scale, int top, int x, int y, int width, int height)
    {
        if (label.LabelCode is not { } labelCode)
        {
            return;
        }

        string code = Clean(labelCode);
        string mode = label.ServiceMode ?? string.Empty;
        string word = label.Express ? "EXPRESS" : mode switch { "ocean" => "SEA", "ground" => "GROUND", _ => "AIR" };
        string At(int left, int down) => string.Create(CultureInfo.InvariantCulture, $"^FO{Dots(left, scale)},{Dots(down + top, scale)}");

        if (mode == "ground")
        {
            zpl.Append(At(x, y)).Append(CultureInfo.InvariantCulture, $"^GB{Dots(6, scale)},{Dots(height, scale)},{Dots(6, scale)}^FS");
        }
        else if (mode == "ocean")
        {
            // Stripes at 45 degrees, clipped to the box, then black plates behind the code and the word.
            for (int line = 20; line < width + height; line += 40)
            {
                int from = Math.Max(0, line - height);
                int to = Math.Min(width, line);
                int length = to - from;

                if (length > 14)
                {
                    zpl.Append(At(x + from, y + line - to)).Append(CultureInfo.InvariantCulture, $"^GD{Dots(length, scale)},{Dots(length, scale)},{Dots(14, scale)},B,R^FS");
                }
            }

            zpl.Append(At(x + 26, y + 46)).Append(CultureInfo.InvariantCulture, $"^GB{Dots(width - 52, scale)},{Dots(122, scale)},{Dots(122, scale)}^FS");
            zpl.Append(At(x + 70, y + 176)).Append(CultureInfo.InvariantCulture, $"^GB{Dots(width - 140, scale)},{Dots(40, scale)},{Dots(40, scale)}^FS");
        }
        else
        {
            zpl.Append(At(x, y)).Append(CultureInfo.InvariantCulture, $"^GB{Dots(width, scale)},{Dots(height, scale)},{Dots(height, scale)}^FS");
        }

        bool reversed = mode != "ground";
        zpl.Append(At(x, y + 56)).Append(CultureInfo.InvariantCulture, $"^A0N,{Dots(110, scale)},{Dots(104, scale)}^FB{Dots(width, scale)},1,0,C,0")
            .Append(reversed ? "^FR" : string.Empty).Append("^FD").Append(code).Append("^FS");
        zpl.Append(At(x, y + 182)).Append(CultureInfo.InvariantCulture, $"^A0N,{Dots(28, scale)},{Dots(28, scale)}^FB{Dots(width, scale)},1,0,C,0")
            .Append(reversed ? "^FR" : string.Empty).Append("^FD").Append(word).Append("^FS");
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
