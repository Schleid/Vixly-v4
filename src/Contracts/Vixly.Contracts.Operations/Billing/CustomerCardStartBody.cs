namespace Vixly.Contracts.Operations.Billing;

// =========================================================
// FILE: CustomerCardStartBody.cs
//
// PURPOSE:
// Asks to pay an invoice by card on the portal (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry where Stripe's page sends the customer back (followed by the charge's id)
//
// IMPORTANT NOTES:
// - An https address on the company's own host
// =========================================================
public sealed record CustomerCardStartBody(string ReturnUrlPrefix);
