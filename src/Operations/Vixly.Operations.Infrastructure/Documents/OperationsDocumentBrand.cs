using QuestPDF.Fluent;

namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsDocumentBrand.cs
//
// PURPOSE:
// The top of the company's documents: its document logo, or its name when it has none (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - The logo about 1.5 cm tall, keeping its proportions, with the company name under it
// - On labels, the black and white copy
//
// IMPORTANT NOTES:
// - The company's own logo and name only, never "Vixly" (ADR-0012 W-1)
// =========================================================
internal static class OperationsDocumentBrand
{
    public const float LogoHeight = 42f;

    public const float LabelLogoHeight = 30f;

    public static void Head(ColumnDescriptor column, byte[]? logo, string companyName, float nameSize)
    {
        ArgumentNullException.ThrowIfNull(column);

        if (Readable(logo) is not { } image)
        {
            column.Item().Text(companyName).FontSize(nameSize).SemiBold();
            return;
        }

        column.Item().Height(LogoHeight).AlignLeft().Image(image).FitHeight();
        column.Item().PaddingTop(2).Text(companyName).FontSize(9).SemiBold();
    }

    // The thermal label: the black and white logo alone, or the name.
    public static void Label(ColumnDescriptor column, byte[]? labelLogo, string companyName, float nameSize)
    {
        ArgumentNullException.ThrowIfNull(column);

        if (Readable(labelLogo) is not { } image)
        {
            column.Item().Text(companyName).FontSize(nameSize).SemiBold();
            return;
        }

        column.Item().Height(LabelLogoHeight).AlignLeft().Image(image).FitHeight();
    }

    // A logo the PDF engine cannot read never stops a document: the company name is printed instead.
    public static QuestPDF.Infrastructure.Image? Readable(byte[]? logo)
    {
        if (logo is null)
        {
            return null;
        }

        try
        {
            return QuestPDF.Infrastructure.Image.FromBinaryData(logo);
        }
        catch (QuestPDF.Drawing.Exceptions.DocumentComposeException)
        {
            return null;
        }
    }
}
