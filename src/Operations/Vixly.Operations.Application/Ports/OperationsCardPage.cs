namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCardPage.cs
//
// PURPOSE:
// Stripe's payment page made for a charge (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry its id (cs_...) and its address
//
// IMPORTANT NOTES:
// - The address is Stripe's; the customer types the card there, never on Vixly
// =========================================================
public sealed record OperationsCardPage(string CheckoutSessionId, string Url);
