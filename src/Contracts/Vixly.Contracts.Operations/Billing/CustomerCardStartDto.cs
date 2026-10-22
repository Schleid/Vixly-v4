namespace Vixly.Contracts.Operations.Billing;

// =========================================================
// FILE: CustomerCardStartDto.cs
//
// PURPOSE:
// A card payment the customer started on the portal (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry the charge's id, to follow it
//
// IMPORTANT NOTES:
// - The card is typed on Stripe's page, never on the portal
// =========================================================
public sealed record CustomerCardStartDto(Guid ChargeId);
