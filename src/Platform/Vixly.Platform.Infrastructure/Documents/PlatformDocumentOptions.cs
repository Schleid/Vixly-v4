namespace Vixly.Platform.Infrastructure.Documents;

// =========================================================
// FILE: PlatformDocumentOptions.cs
//
// PURPOSE:
// Settings for rendering billing PDFs.
//
// RESPONSIBILITIES:
// - Bind the PlatformDocuments section
//
// IMPORTANT NOTES:
// - The QuestPDF licence type is the company's choice (Community is free below USD 1M yearly revenue); it lives in configuration, not code
// =========================================================
public sealed class PlatformDocumentOptions
{
    public const string SectionName = "PlatformDocuments";

    public string QuestPdfLicense { get; set; } = string.Empty;
}
