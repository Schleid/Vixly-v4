namespace Vixly.Operations.Web.Site;

// =========================================================
// FILE: OperationsSiteEstimate.cs
//
// PURPOSE:
// A visitor's estimate on the home page rates section (owner, 2026-10-30): what was asked and the answer.
//
// RESPONSIBILITIES:
// - Carry the service, destination and weight back into the form, and the estimate when priced
//
// IMPORTANT NOTES:
// - Result null: no published price for that choice; the page says so and never shows a made-up amount
// =========================================================
public sealed record OperationsSiteEstimate(string? Service, Guid? DestinationId, decimal? Weight, Vixly.Operations.Application.Charges.OperationsEstimate? Result);
