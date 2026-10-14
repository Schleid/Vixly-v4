namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: PlatformPlanChoice.cs
//
// PURPOSE:
// The outcome of choosing a plan at registration.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Step is pending_account_setup after a trial, pending_payment otherwise
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformPlanChoice(string Step, DateTimeOffset? TrialEndsUtc);
