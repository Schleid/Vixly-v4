namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformZelleWaitingClaim.cs
//
// PURPOSE:
// A Zelle payment the customer reported that Vixly has not checked yet.
//
// RESPONSIBILITIES:
// - Tell the checkout page a report is already waiting
//
// IMPORTANT NOTES:
// - No staff data
// =========================================================
public sealed record PlatformZelleWaitingClaim(decimal Amount, DateTime SentOn, DateTimeOffset ReportedUtc);
