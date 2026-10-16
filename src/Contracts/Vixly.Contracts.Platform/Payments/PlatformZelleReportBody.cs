namespace Vixly.Contracts.Platform.Payments;

// =========================================================
// FILE: PlatformZelleReportBody.cs
//
// PURPOSE:
// The customer's "I paid with Zelle" report.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - SentOn is a date (yyyy-MM-dd); Reference is the Zelle confirmation code
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformZelleReportBody(decimal Amount, DateTime? SentOn, string? SenderName, string? Reference);
