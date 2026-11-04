using Vixly.Shared.Kernel.Localization;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsReportMail.cs
//
// PURPOSE:
// A scheduled report email (Slice 11).
//
// RESPONSIBILITIES:
// - Carry the recipient, the company, the report title and period, and the file
//
// IMPORTANT NOTES:
// - The file is made for this recipient’s own rights
// =========================================================
public sealed record OperationsReportMail(string ToAddress, string ToName, LanguageCode Language, string CompanyName, string ReportTitle, string Period, string FileName,
    string ContentType, byte[] Content);
