namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsDocumentOptions.cs
//
// PURPOSE:
// Settings of the documents Operations prints (Slice 3B).
//
// RESPONSIBILITIES:
// - Bind the OperationsDocuments configuration section
//
// IMPORTANT NOTES:
// - The QuestPDF licence type is the company's choice (Community is free below USD 1M yearly revenue); it lives in configuration, as in Platform
// =========================================================
public sealed class OperationsDocumentOptions
{
    public const string SectionName = "OperationsDocuments";

    public string QuestPdfLicense { get; set; } = string.Empty;
}
