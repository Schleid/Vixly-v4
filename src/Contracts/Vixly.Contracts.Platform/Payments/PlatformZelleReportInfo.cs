namespace Vixly.Contracts.Platform.Payments;

// =========================================================
// FILE: PlatformZelleReportInfo.cs
//
// PURPOSE:
// A Zelle payment the customer reported that Vixly is still checking.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformZelleReportInfo(decimal Amount, DateTime SentOn, DateTimeOffset ReportedUtc);
