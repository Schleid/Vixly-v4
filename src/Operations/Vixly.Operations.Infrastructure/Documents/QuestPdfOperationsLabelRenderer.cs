using System.Globalization;
using System.Text;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: QuestPdfOperationsLabelRenderer.cs
//
// PURPOSE:
// Renders the 2 x 1 inch parcel ID label and the warehouse receipt as PDF (Slice 3B, D-R3).
//
// RESPONSIBILITIES:
// - Draw the parcel ID as a Code 128 barcode with the customer code, for stations that print from the browser
// - Lay out the receipt of a carrier delivery for the driver to sign
// - Draw the 4 x 6 inch shipping label (Slice 4D step 2)
// - Lay out a shipment's packing list and pro forma commercial invoice (Slice 5A)
//
// IMPORTANT NOTES:
// - Dock printers get the same label in ZPL (OperationsZplLabels); this PDF is the fallback
// - QuestPDF licence type from configuration, as in Platform; bundled fonts, so every host prints the same
// - Receipt words come from OperationsText in the reader's language
// =========================================================
internal sealed class QuestPdfOperationsLabelRenderer : IOperationsLabelRenderer
{
    private readonly IStringLocalizer<OperationsText> _text;

    // Null outside the host (the infrastructure tests): documents show the company name.
    private readonly OperationsDocumentLogos? _logos;

    public QuestPdfOperationsLabelRenderer(IOptions<OperationsDocumentOptions> options, IStringLocalizer<OperationsText> text, OperationsDocumentLogos? logos = null)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _logos = logos;
        string license = options?.Value.QuestPdfLicense ?? string.Empty;
        QuestPDF.Settings.License = Enum.TryParse(license, ignoreCase: true, out LicenseType type) ? type : LicenseType.Community;
    }

    public byte[] RenderParcelLabel(OperationsParcelLabel label)
    {
        ArgumentNullException.ThrowIfNull(label);
        return Document.Create(document => ParcelLabelPage(document, label)).GeneratePdf();
    }

    // Owner, 2026-10-27: every ID label of a shipment in one document, one label per page, printed at once.
    public byte[] RenderParcelLabels(IReadOnlyList<OperationsParcelLabel> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);
        return Document.Create(document =>
        {
            foreach (OperationsParcelLabel label in labels)
            {
                ParcelLabelPage(document, label);
            }
        }).GeneratePdf();
    }

    private static void ParcelLabelPage(IDocumentContainer document, OperationsParcelLabel label)
    {
        string barcode = BarcodeSvg(label.ParcelNumber, 4);
        string code = string.IsNullOrWhiteSpace(label.CustomerCode) ? Vixly.Operations.Application.Printing.OperationsZplLabels.Unidentified : label.CustomerCode;

        document.Page(page =>
        {
            page.Size(new PageSize(2 * 72, 1 * 72));
            page.Margin(4);
            page.DefaultTextStyle(text => text.FontSize(7).FontColor("#000000"));
            page.Content().Column(column =>
            {
                column.Spacing(1);
                column.Item().Svg(barcode).FitWidth();
                column.Item().Text(label.ParcelNumber).FontSize(7);
                column.Item().Text(code).FontSize(code.Length > 8 ? 11 : 16).Bold();
            });
        });
    }

    // Slice 12A: the badge card. The code is printed under its barcode only in part, so a photo of the card is not enough to type it.
    public byte[] RenderStaffBadge(Vixly.Operations.Application.Identity.OperationsStaffBadgeCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        string barcode = BarcodeSvg(card.Code, 5);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(new PageSize(85.6f, 54f, Unit.Millimetre));
            page.Margin(4, Unit.Millimetre);
            page.DefaultTextStyle(text => text.FontSize(8).FontColor("#000000"));
            page.Content().Column(column =>
            {
                column.Spacing(2);
                column.Item().Text(card.CompanyName).FontSize(9).SemiBold().ClampLines(1);
                column.Item().PaddingTop(2).Text(card.PersonName).FontSize(13).Bold().ClampLines(2);
                column.Item().PaddingTop(4).Height(16, Unit.Millimetre).Svg(barcode).FitArea();
                column.Item().AlignRight().Text("\u2026" + card.Code[^4..]).FontSize(6).FontColor("#555555");
            });
        })).GeneratePdf();
    }

    // Slice 4D step 2: the 4 x 6 inch shipping label, the same content as the ZPL one.
    public byte[] RenderShippingLabel(OperationsShippingLabel label)
    {
        ArgumentNullException.ThrowIfNull(label);
        return Document.Create(document => ShippingLabelPage(document, label)).GeneratePdf();
    }

    // Owner, 2026-10-27: the shipping labels of a shipment in one document, one per page, printed at once.
    public byte[] RenderShippingLabels(IReadOnlyList<OperationsShippingLabel> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);
        return Document.Create(document =>
        {
            foreach (OperationsShippingLabel label in labels)
            {
                ShippingLabelPage(document, label);
            }
        }).GeneratePdf();
    }

    // Owner, 2026-10-31: the same layout as the ZPL label: logo and ship date; the route beside the service's label code in its box (air solid,
    // ocean striped, ground outlined; express banded); the recipient; the tracking barcode; weight, flags and piece; the parcel barcode with its
    // number; the shipment number at the foot. No customer number and no contents (owner, 2026-10-31).
    private void ShippingLabelPage(IDocumentContainer document, OperationsShippingLabel label)
    {
        // Owner, 2026-10-28: the tracking number is the label's main barcode once the shipment is paid, on account or collect.
        // Owner, 2026-11-01: wide enough to fill the row at about 54 points high.
        string shipmentBarcode = BarcodeSvg(label.TrackingNumber ?? label.ShipmentNumber, 5);
        string parcelBarcode = BarcodeSvg(label.ParcelNumber, 3);
        byte[]? labelLogo = _logos?.Label;
        const string Black = "#000000";
        const string White = "#FFFFFF";
        const float Rule = 2.2f;

        document.Page(page =>
        {
            page.Size(new PageSize(4 * 72, 6 * 72));
            page.Margin(0);
            page.DefaultTextStyle(text => text.FontSize(9).FontColor(Black));
            page.Content().Column(column =>
            {
                if (label.Express)
                {
                    column.Item().Background(Black).PaddingVertical(2).AlignCenter().Text("E X P R E S S").FontSize(12).Bold().FontColor(White);
                }

                column.Item().BorderBottom(Rule).PaddingHorizontal(11).PaddingVertical(6).Row(row =>
                {
                    row.RelativeItem().AlignMiddle().Column(brand => OperationsDocumentBrand.Label(brand, labelLogo, label.CompanyName, 12));
                    row.AutoItem().AlignMiddle().AlignRight().Column(right =>
                    {
                        right.Item().AlignRight().Text(label.ShipDate is { } date ? "Ship date " + date : string.Empty).FontSize(8);
                        right.Item().AlignRight().Text(label.OriginName ?? label.OriginCode).FontSize(8);
                    });
                });
                column.Item().BorderBottom(Rule).Height(92).Row(row =>
                {
                    row.RelativeItem().PaddingHorizontal(11).AlignMiddle().Column(route =>
                    {
                        route.Item().Height(36).AlignMiddle().ScaleToFit().Text(label.OriginCode + " → " + label.DestinationCode).FontSize(30).Bold();
                        route.Item().PaddingTop(4).Text(label.ServiceName ?? string.Empty).FontSize(11).SemiBold().ClampLines(1);
                    });
                    row.ConstantItem(100).Element(box => ServiceBox(box, label));
                });
                column.Item().BorderBottom(Rule).PaddingHorizontal(11).PaddingVertical(6).Column(to =>
                {
                    to.Item().Text("TO").FontSize(7);
                    to.Item().Text(label.RecipientName.ToUpperInvariant()).FontSize(17).Bold().ClampLines(1);
                    to.Item().Row(city =>
                    {
                        city.RelativeItem().Text(label.RecipientCity).FontSize(15).SemiBold().ClampLines(1);
                        city.ConstantItem(70).AlignMiddle().Background(label.Pickup ? Black : White).Border(1.5f).BorderColor(Black).PaddingVertical(2)
                            .AlignCenter().Text(label.Pickup ? "PICKUP" : "HOME").FontSize(10).Bold().FontColor(label.Pickup ? White : Black);
                    });
                });
                column.Item().BorderBottom(Rule).PaddingHorizontal(11).PaddingVertical(6).Column(main =>
                {
                    main.Item().Svg(shipmentBarcode).FitWidth();
                    main.Item().PaddingTop(2).AlignCenter().Text(label.TrackingNumber is { } tracking
                        ? Vixly.Operations.Domain.Shipments.OperationsTrackingNumbers.Format(tracking)
                        : label.ShipmentNumber).FontSize(15).Bold();
                });
                column.Item().BorderBottom(Rule).PaddingHorizontal(11).PaddingVertical(5).Row(row =>
                {
                    row.RelativeItem().Column(weight =>
                    {
                        weight.Item().Text("Weight").FontSize(8).SemiBold();
                        weight.Item().Text(label.Weight).FontSize(15).Bold();
                    });
                    row.RelativeItem().AlignCenter().AlignMiddle().Text(string.Join("   ",
                        new[] { label.Insured ? "INSURED" : null, label.Consolidated ? "CONSOL" : null }.Where(flag => flag is not null))).FontSize(10).Bold();
                    row.RelativeItem().AlignRight().Column(piece =>
                    {
                        piece.Item().AlignRight().Text("Piece").FontSize(8).SemiBold();
                        piece.Item().AlignRight().Text(string.Create(CultureInfo.InvariantCulture, $"{label.Piece} / {label.Pieces}")).FontSize(20).Bold();
                    });
                });
                column.Item().PaddingHorizontal(11).PaddingVertical(6).Column(parcel =>
                {
                    parcel.Item().Height(40).AlignLeft().Svg(parcelBarcode).FitHeight();
                    parcel.Item().PaddingTop(2).Text(label.ParcelNumber).FontSize(12).Bold();
                });
            });

            // Owner, 2026-10-31: the shipment number at the foot of the label, in print only, never as a barcode.
            page.Footer().PaddingHorizontal(11).PaddingBottom(8).AlignRight().Text("Shipment " + label.ShipmentNumber).FontSize(13).Bold();
        });
    }

    // The service's label code, large: air solid, ocean striped (the code on black plates), ground outlined.
    private static void ServiceBox(IContainer box, OperationsShippingLabel label)
    {
        if (label.LabelCode is not { } code)
        {
            return;
        }

        string mode = label.ServiceMode ?? string.Empty;
        string word = label.Express ? "EXPRESS" : mode switch { "ocean" => "SEA", "ground" => "GROUND", _ => "AIR" };

        if (mode == "ground")
        {
            box.BorderLeft(2.2f).AlignCenter().AlignMiddle().Column(text =>
            {
                text.Item().AlignCenter().Text(code).FontSize(36).Bold();
                text.Item().AlignCenter().Text(word).FontSize(9).Bold().LetterSpacing(0.1f);
            });
            return;
        }

        if (mode == "ocean")
        {
            box.Layers(layers =>
            {
                layers.Layer().Svg(Stripes(100, 92)).FitArea();
                layers.PrimaryLayer().AlignCenter().AlignMiddle().Column(text =>
                {
                    text.Item().AlignCenter().Background("#000000").PaddingHorizontal(4).Text(code).FontSize(36).Bold().FontColor("#FFFFFF");
                    text.Item().AlignCenter().Background("#000000").PaddingHorizontal(4).Text(word).FontSize(9).Bold().LetterSpacing(0.1f).FontColor("#FFFFFF");
                });
            });
            return;
        }

        box.Background("#000000").AlignCenter().AlignMiddle().Column(text =>
        {
            text.Item().AlignCenter().Text(code).FontSize(36).Bold().FontColor("#FFFFFF");
            text.Item().AlignCenter().Text(word).FontSize(9).Bold().LetterSpacing(0.1f).FontColor("#FFFFFF");
        });
    }

    // Black stripes at 45 degrees on white, for the ocean box.
    private static string Stripes(int width, int height)
    {
        StringBuilder svg = new();
        svg.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">");
        svg.Append(CultureInfo.InvariantCulture, $"<defs><clipPath id=\"box\"><rect width=\"{width}\" height=\"{height}\"/></clipPath></defs><g clip-path=\"url(#box)\">");

        for (int line = 0; line < width + height + 20; line += 14)
        {
            svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{line}\" y1=\"-10\" x2=\"{line - height - 20}\" y2=\"{height + 10}\" stroke=\"#000\" stroke-width=\"5\"/>");
        }

        return svg.Append("</g></svg>").ToString();
    }

    public byte[] RenderReceipt(OperationsReceiptDocument receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        string Word(string key) => _text["ui.receipt." + key].Value;

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(36);
            page.DefaultTextStyle(text => text.FontSize(10).FontColor("#1F2933"));
            page.Header().Column(column =>
            {
                OperationsDocumentBrand.Head(column, _logos?.Logo, receipt.CompanyName, 16);
                column.Item().Text(VixlyText.Format(_text, "ui.receipt.title", "number", receipt.ReceiptNumber)).FontSize(13);
                column.Item().PaddingTop(4).Text(receipt.WarehouseCode + " · " + receipt.WarehouseName);
            });
            page.Content().PaddingVertical(12).Column(column =>
            {
                column.Spacing(6);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(140);
                        columns.RelativeColumn();
                    });
                    void Row(string name, string value)
                    {
                        table.Cell().Text(Word(name)).SemiBold();
                        table.Cell().Text(value);
                    }

                    Row("carrier", receipt.Carrier ?? "—");
                    Row("opened", receipt.OpenedUtc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture));
                    Row("closed", receipt.ClosedUtc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture));
                    Row("received_by", receipt.ReceivedBy);
                    Row("pieces", receipt.PieceCount.ToString(CultureInfo.InvariantCulture));
                    Row("damaged", receipt.DamagedCount.ToString(CultureInfo.InvariantCulture));
                });
                column.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(28);
                        columns.ConstantColumn(100);
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(70);
                    });
                    table.Header(header =>
                    {
                        foreach (string name in new[] { "line", "parcel", "tracking", "customer", "condition" })
                        {
                            header.Cell().BorderBottom(1).PaddingBottom(2).Text(Word("column." + name)).SemiBold();
                        }
                    });

                    int index = 0;

                    foreach (OperationsReceiptLine line in receipt.Lines)
                    {
                        index++;
                        table.Cell().Text(index.ToString(CultureInfo.InvariantCulture));
                        table.Cell().Text(line.ParcelNumber);
                        table.Cell().Text(line.TrackingNumber ?? string.Empty);
                        table.Cell().Text(line.CustomerCode ?? Word("unidentified"));
                        table.Cell().Text(line.Damaged ? Word("damaged_short") : string.Empty);
                    }
                });
            });
            page.Footer().Row(row =>
            {
                row.Spacing(24);
                row.RelativeItem().Column(column =>
                {
                    column.Item().PaddingTop(24).BorderTop(1).Text(Word("driver_signature"));
                    column.Item().Text(Word("driver_name"));
                });
                row.RelativeItem().Column(column =>
                {
                    column.Item().PaddingTop(24).BorderTop(1).Text(Word("clerk_signature"));
                });
            });
        })).GeneratePdf();
    }

    // Slice 5A: the packing list or the pro forma commercial invoice of a shipment, on a Letter page.
    public byte[] RenderShipmentDocument(Vixly.Operations.Application.Shipments.OperationsShipmentDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        string Word(string key) => _text["ui.shipment_doc." + key].Value;
        bool invoice = document.Kind == Vixly.Operations.Application.Shipments.OperationsShipmentDocumentKind.CommercialInvoice;
        string barcode = BarcodeSvg(document.ShipmentNumber, 6);
        string service = document.ServiceLabel
            ?? (document.ServiceCode is { } code && !_text["ui.services.name." + code].ResourceNotFound ? _text["ui.services.name." + code].Value : document.ServiceCode ?? "—");
        string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture) + " " + document.CurrencyCode;

        return Document.Create(pdf => pdf.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(36);
            page.DefaultTextStyle(text => text.FontSize(9.5f).FontColor("#1F2933"));
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    OperationsDocumentBrand.Head(column, _logos?.Logo, document.CompanyName, 15);
                    column.Item().Text(Word(invoice ? "invoice_title" : "packing_title")).FontSize(13);
                    column.Item().Text(Word("number") + " " + document.ShipmentNumber + " · " + Word("date") + " " + document.IssuedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                });
                row.ConstantItem(170).Height(46).Svg(barcode).FitArea();
            });
            page.Content().PaddingVertical(12).Column(column =>
            {
                column.Spacing(8);
                column.Item().Row(row =>
                {
                    row.Spacing(12);
                    void Party(string title, IReadOnlyList<string> lines) => row.RelativeItem().Border(0.75f).Padding(6).Column(box =>
                    {
                        box.Item().Text(Word(title)).SemiBold();

                        foreach (string line in lines.Where(line => !string.IsNullOrWhiteSpace(line)))
                        {
                            box.Item().Text(line);
                        }
                    });

                    // The packing list names the recipient only; the shipper is on the invoice (review).
                    if (invoice)
                    {
                        Party("shipper", document.Shipper);
                    }

                    Party("consignee", document.Consignee);
                });
                column.Item().Text(Word("origin") + ": " + document.Origin + "    " + Word("destination") + ": " + document.Destination + "    " + Word("service") + ": " + service);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(24);
                        columns.ConstantColumn(90);
                        columns.RelativeColumn();
                        columns.ConstantColumn(62);
                        columns.ConstantColumn(100);

                        if (invoice)
                        {
                            columns.ConstantColumn(80);
                        }
                    });
                    table.Header(header =>
                    {
                        foreach (string name in invoice ? new[] { "line", "parcel", "contents", "weight", "size", "value" } : new[] { "line", "parcel", "contents", "weight", "size" })
                        {
                            header.Cell().BorderBottom(1).PaddingBottom(2).Text(Word("column." + name)).SemiBold();
                        }
                    });

                    int index = 0;

                    foreach (Vixly.Operations.Application.Shipments.OperationsShipmentDocumentLine line in document.Lines)
                    {
                        index++;
                        table.Cell().Text(index.ToString(CultureInfo.InvariantCulture));
                        table.Cell().Text(line.ParcelNumber);
                        table.Cell().Text(line.Contents);
                        table.Cell().Text(line.Weight ?? "—");
                        table.Cell().Text(line.Size ?? "—");

                        if (invoice)
                        {
                            table.Cell().AlignRight().Text(line.DeclaredValue is { } value ? Money(value) : "—");
                        }
                    }
                });
                column.Item().AlignRight().Text(VixlyText.Format(_text, "ui.shipment_doc.totals", new Dictionary<string, object>
                {
                    ["pieces"] = document.Lines.Count,
                    ["weight"] = document.TotalWeight
                })).SemiBold();

                if (invoice)
                {
                    column.Item().AlignRight().Text(Word("total_value") + ": " + Money(document.TotalValue)).FontSize(11).SemiBold();
                    column.Item().PaddingTop(6).Text(Word("pro_forma"));
                    column.Item().Text(Word("reason") + ": ____________________________");
                }
            });
            page.Footer().ShowIf(invoice).Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().PaddingTop(24).BorderTop(1).Text(Word("shipper_signature"));
                });
                row.ConstantItem(24);
                row.RelativeItem().Column(column =>
                {
                    column.Item().PaddingTop(24).BorderTop(1).Text(Word("date_signed"));
                });
            });
        })).GeneratePdf();
    }

    // Slice 5B: a shipment's insurance certificate, on a Letter page.
    // Slice 5E: 30 labels of 2.625 x 1 inch on Letter (the common address-label sheet), filled from the top left.
    public byte[] RenderSupplyLabels(Vixly.Operations.Application.Supplies.OperationsSupplyLabel label, int count)
    {
        ArgumentNullException.ThrowIfNull(label);
        string price = label.Price is { } amount ? amount.ToString("0.00", CultureInfo.InvariantCulture) + " " + label.CurrencyCode : string.Empty;
        int labels = Math.Clamp(count, 1, 30);

        return Document.Create(pdf => pdf.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.MarginTop(36);
            page.MarginHorizontal(13.5f);
            page.DefaultTextStyle(text => text.FontSize(7).FontColor("#1F2933"));
            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(189);
                    columns.ConstantColumn(9);
                    columns.ConstantColumn(189);
                    columns.ConstantColumn(9);
                    columns.ConstantColumn(189);
                });

                for (int index = 0; index < 30; index++)
                {
                    if (index % 3 != 0)
                    {
                        table.Cell().Height(72);
                    }

                    if (index < labels)
                    {
                        table.Cell().Height(72).Padding(6).Column(column =>
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text(label.Name).SemiBold().ClampLines(1);
                                row.AutoItem().Text(price).SemiBold();
                            });
                            column.Item().Height(36).Svg(BarcodeSvg(label.Code, 6)).FitArea();
                            column.Item().AlignCenter().Text(label.Code);
                        });
                    }
                    else
                    {
                        table.Cell().Height(72);
                    }
                }
            });
        })).GeneratePdf();
    }

    public byte[] RenderInsuranceCertificate(Vixly.Operations.Application.Insurance.OperationsCertificateDocument certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        string Word(string key) => _text["ui.certificate." + key].Value;
        string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture) + " " + certificate.CurrencyCode;
        string barcode = BarcodeSvg(certificate.CertificateNumber, 6);
        string service = certificate.ServiceCode is { } code && !_text["ui.services.name." + code].ResourceNotFound ? _text["ui.services.name." + code].Value : _text["ui.certificate.mode." + certificate.Mode].Value;
        IEnumerable<string> Flags<T>(T value, string prefix)
            where T : struct, Enum => Enum.GetValues<T>().Where(flag => Convert.ToInt64(flag, CultureInfo.InvariantCulture) != 0 && value.HasFlag(flag)
                && System.Numerics.BitOperations.PopCount((ulong)Convert.ToInt64(flag, CultureInfo.InvariantCulture)) == 1)
                .Select(flag => _text[prefix + flag].Value);

        return Document.Create(pdf => pdf.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(36);
            page.DefaultTextStyle(text => text.FontSize(10).FontColor("#1F2933"));
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    OperationsDocumentBrand.Head(column, _logos?.Logo, certificate.CompanyName, 15);
                    column.Item().Text(Word("title")).FontSize(13);
                    column.Item().Text(Word("number") + " " + certificate.CertificateNumber + " · " + Word("issued") + " " + certificate.IssuedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    column.Item().Text(Word("status." + certificate.Status)).SemiBold();
                });
                row.ConstantItem(170).Height(46).Svg(barcode).FitArea();
            });
            page.Content().PaddingVertical(12).Column(column =>
            {
                column.Spacing(6);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(150);
                        columns.RelativeColumn();
                    });
                    void Row(string name, string value)
                    {
                        table.Cell().PaddingVertical(2).Text(Word(name)).SemiBold();
                        table.Cell().PaddingVertical(2).Text(value);
                    }

                    Row("insured", certificate.Insured);
                    Row("consignee", certificate.Consignee);
                    Row("shipment", certificate.ShipmentNumber);
                    Row("route", certificate.Route + " · " + service);
                    Row("insurer", certificate.SelfInsured
                        ? VixlyText.Format(_text, "ui.certificate.self_insured", "company", certificate.CompanyName)
                        : (certificate.InsurerName ?? "—") + (certificate.PolicyNumber is { } number ? " · " + Word("policy") + " " + number : string.Empty));
                    Row("risks", string.Join(", ", Flags(certificate.Risks, "ui.insurance.risk.")));
                    Row("exclusions", string.Join(", ", Flags(certificate.Exclusions, "ui.insurance.exclusion.").Concat(certificate.OtherExclusions is { } other
                        ? other.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : Array.Empty<string>())));
                    Row("payout", _text["ui.insurance.payout." + certificate.Payout].Value);

                    if (certificate.DeductibleAmount is { } deductible)
                    {
                        Row("deductible", Money(deductible));
                    }
                    else if (certificate.DeductiblePercent is { } share)
                    {
                        Row("deductible", share.ToString("0.###", CultureInfo.InvariantCulture) + " %");
                    }

                    if (certificate.MaxValue is { } max)
                    {
                        Row("max_value", Money(max));
                    }

                    Row("declared_value", Money(certificate.DeclaredValue));
                    Row("premium", certificate.Premium is { } premium ? Money(premium) : Word("not_insurable"));
                    Row("accepted", certificate.AcceptedUtc.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture));
                });

                if (certificate.Status == Vixly.Operations.Domain.Insurance.OperationsCertificateStatus.Provisional)
                {
                    column.Item().PaddingTop(8).Text(Word("provisional_note"));
                }
            });
            page.Footer().Text(Word("footer")).FontSize(8);
        })).GeneratePdf();
    }

    // Slice 5D: the customer's declaration and insurance choice, to sign at the counter, on a Letter page.
    public byte[] RenderCustomerDeclaration(Vixly.Operations.Application.Shipments.OperationsCustomerDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        string Word(string key) => _text["ui.declaration." + key].Value;
        string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture) + " " + declaration.CurrencyCode;
        string service = declaration.ServiceCode is { } code && !_text["ui.services.name." + code].ResourceNotFound ? _text["ui.services.name." + code].Value : string.Empty;

        return Document.Create(pdf => pdf.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(36);
            page.DefaultTextStyle(text => text.FontSize(10).FontColor("#1F2933"));
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    OperationsDocumentBrand.Head(column, _logos?.Logo, declaration.CompanyName, 15);
                    column.Item().Text(Word("title")).FontSize(13);
                    column.Item().Text(Word("shipment") + " " + declaration.ShipmentNumber + " · " + declaration.IssuedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                });
                row.ConstantItem(170).Height(46).Svg(BarcodeSvg(declaration.ShipmentNumber, 6)).FitArea();
            });
            page.Content().PaddingVertical(12).Column(column =>
            {
                column.Spacing(8);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(130);
                        columns.RelativeColumn();
                    });
                    void Row(string name, string value)
                    {
                        table.Cell().PaddingVertical(2).Text(Word(name)).SemiBold();
                        table.Cell().PaddingVertical(2).Text(value);
                    }

                    Row("customer", declaration.Customer);
                    Row("recipient", declaration.Recipient);
                    Row("route", declaration.Route + (service.Length > 0 ? " · " + service : string.Empty));
                });
                column.Item().Text(Word("contents")).FontSize(12).SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(100);
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(90);
                    });
                    table.Header(header =>
                    {
                        foreach (string name in new[] { "parcel", "description", "weight", "value" })
                        {
                            header.Cell().BorderBottom(1).PaddingVertical(3).Text(Word(name)).SemiBold();
                        }
                    });

                    foreach (Vixly.Operations.Application.Shipments.OperationsDeclarationLine line in declaration.Lines)
                    {
                        table.Cell().PaddingVertical(3).Text(line.Number);
                        table.Cell().PaddingVertical(3).Text(line.Contents);
                        table.Cell().PaddingVertical(3).AlignRight().Text(line.Weight);
                        table.Cell().PaddingVertical(3).AlignRight().Text(Money(line.Value));
                    }

                    table.Cell().BorderTop(1).PaddingVertical(3).Text(Word("total")).SemiBold();
                    table.Cell().BorderTop(1).PaddingVertical(3).Text(string.Empty);
                    table.Cell().BorderTop(1).PaddingVertical(3).AlignRight().Text(declaration.TotalWeight).SemiBold();
                    table.Cell().BorderTop(1).PaddingVertical(3).AlignRight().Text(Money(declaration.TotalValue)).SemiBold();
                });
                column.Item().Text(Word("declare")).FontSize(9);
                column.Item().Text(Word("insurance")).FontSize(12).SemiBold();

                if (declaration.InsuranceName is { } insurance)
                {
                    column.Item().Text(insurance + (declaration.InsuredValue is { } insured ? " · " + Word("insured_value") + " " + Money(insured) : string.Empty)
                        + (declaration.Premium is { } premium ? " · " + Word("premium") + " " + Money(premium) : string.Empty)).SemiBold();

                    if (declaration.Coverage is { } coverage)
                    {
                        IEnumerable<string> risks = Enum.GetValues<OperationsInsuredRisks>().Where(risk => risk != OperationsInsuredRisks.None && coverage.Risks.HasFlag(risk))
                            .Select(risk => _text["ui.insurance.risk." + risk].Value);
                        column.Item().Text(VixlyText.Format(_text, "ui.insurance_choice.covers", "risks", string.Join(", ", risks)));
                        column.Item().Text(coverage.Cover == OperationsInsuranceCover.Insurer && coverage.InsurerName is { } insurer
                            ? VixlyText.Format(_text, "ui.insurance_choice.insurer", "insurer", insurer)
                            : _text["ui.insurance_choice.own"].Value);

                        if (coverage.DeductibleAmount is > 0 and { } deductible)
                        {
                            column.Item().Text(VixlyText.Format(_text, "ui.insurance_choice.deductible", "amount", Money(deductible)));
                        }

                        column.Item().Text(VixlyText.Format(_text, "ui.insurance_choice.claim_days", new Dictionary<string, object>
                        {
                            ["damage"] = coverage.DamageClaimDays, ["loss"] = coverage.LossClaimDays
                        }));

                        // The policy's own words, as the customer read them before signing (on paper or on the customer screen).
                        if (coverage.Terms(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) is { } policy)
                        {
                            column.Item().PaddingTop(4).Text(policy).FontSize(8);
                        }
                    }

                    column.Item().Text(Word("accept")).FontSize(9);
                }
                else
                {
                    column.Item().Text(Word("no_insurance")).SemiBold();

                    if (declaration.LiabilityText is { } liability)
                    {
                        column.Item().Text(liability);
                    }
                }

                column.Item().PaddingTop(18).Row(row =>
                {
                    row.RelativeItem().Column(signature =>
                    {
                        if (Picture(declaration.Signature) is { } image)
                        {
                            signature.Item().Height(60).Image(image).FitArea();
                        }
                        else
                        {
                            signature.Item().Height(60);
                        }

                        signature.Item().BorderTop(1).Text(Word("signature"));
                    });
                    row.ConstantItem(24);
                    row.ConstantItem(160).Column(date =>
                    {
                        date.Item().Height(60).AlignBottom().Text(declaration.SignedUtc?.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture) ?? string.Empty);
                        date.Item().BorderTop(1).Text(Word("date"));
                    });
                });
            });
            page.Footer().Text(Word("footer")).FontSize(8);
        })).GeneratePdf();
    }

    // Slice 5F: an invoice or a credit note.
    public byte[] RenderInvoice(Vixly.Operations.Application.Billing.OperationsInvoiceDocument document) =>
        OperationsInvoiceLayout.Render(document ?? throw new ArgumentNullException(nameof(document)), _text, number => BarcodeSvg(number, 6), _logos?.Logo);

    public byte[] RenderQuote(Vixly.Operations.Application.Charges.OperationsQuoteDocument document) =>
        OperationsQuoteLayout.Render(document ?? throw new ArgumentNullException(nameof(document)), _text, number => BarcodeSvg(number, 4), _logos?.Logo);

    public byte[] RenderPaymentReceipt(Vixly.Operations.Application.Billing.OperationsPaymentReceipt receipt) =>
        OperationsPaymentReceiptLayout.Render(receipt ?? throw new ArgumentNullException(nameof(receipt)), _text, number => BarcodeSvg(number, 6), _logos?.Logo);

    public byte[] RenderBundleLabel(Vixly.Operations.Application.Bundles.OperationsBundleLabel label) =>
        OperationsBundleLabelLayout.Render(label ?? throw new ArgumentNullException(nameof(label)), _text, number => BarcodeSvg(number, 4));

    public byte[] RenderRepackBoxLabel(Vixly.Operations.Application.Bundles.OperationsRepackBoxLabel label) =>
        OperationsBundleLabelLayout.Render(label ?? throw new ArgumentNullException(nameof(label)), _text, number => BarcodeSvg(number, 4));

    public byte[] RenderDispatchManifest(Vixly.Operations.Application.Dispatch.OperationsDispatchManifest manifest) =>
        OperationsManifestLayout.RenderDispatch(manifest ?? throw new ArgumentNullException(nameof(manifest)), _text, _logos?.Logo);

    public byte[] RenderBundleManifest(Vixly.Operations.Application.Dispatch.OperationsBundleManifest manifest) =>
        OperationsManifestLayout.RenderBundle(manifest ?? throw new ArgumentNullException(nameof(manifest)), _text, _logos?.Logo);

    public byte[] RenderHouseWaybills(IReadOnlyList<Vixly.Operations.Application.Dispatch.OperationsHouseWaybill> waybills) =>
        OperationsWaybillLayout.RenderHouseWaybills(waybills ?? throw new ArgumentNullException(nameof(waybills)), _text, _logos?.Logo);

    public byte[] RenderShipperLetter(Vixly.Operations.Application.Dispatch.OperationsShipperLetter letter) =>
        OperationsWaybillLayout.RenderShipperLetter(letter ?? throw new ArgumentNullException(nameof(letter)), _text, _logos?.Logo);

    public byte[] RenderRunSheet(Vixly.Operations.Application.Delivery.OperationsRunSheet sheet) =>
        OperationsRunSheetLayout.Render(sheet ?? throw new ArgumentNullException(nameof(sheet)), _text, _logos?.Logo);

    public byte[] RenderCreditStatement(Vixly.Operations.Application.Billing.OperationsCreditStatement statement) =>
        OperationsCreditStatementLayout.Render(statement ?? throw new ArgumentNullException(nameof(statement)), _text, _logos?.Logo);

    // A stored signature that cannot be read is left out rather than failing the page (Slice 5D).
    private static Image? Picture(byte[]? content)
    {
        if (content is not { Length: > 0 })
        {
            return null;
        }

        try
        {
            return Image.FromBinaryData(content);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    // Bars as rectangles, height a given fraction of the width; ten modules of quiet zone on each side.
    private static string BarcodeSvg(string text, int widthPerHeight)
    {
        IReadOnlyList<int> widths = OperationsCode128.Widths(text);
        int total = widths.Sum() + 20;
        int height = Math.Max(20, total / widthPerHeight);
        StringBuilder svg = new();
        svg.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {total} {height}\">");
        int x = 10;

        for (int index = 0; index < widths.Count; index++)
        {
            if (index % 2 == 0)
            {
                svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{x}\" y=\"0\" width=\"{widths[index]}\" height=\"{height}\" fill=\"#000\"/>");
            }

            x += widths[index];
        }

        return svg.Append("</svg>").ToString();
    }
}
