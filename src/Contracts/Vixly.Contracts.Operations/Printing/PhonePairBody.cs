namespace Vixly.Contracts.Operations.Printing;

// =========================================================
// FILE: PhonePairBody.cs
//
// PURPOSE:
// What Vixly Print sends to join a warehouse (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Carry the one-time code from the QR code and the phone's own name
//
// IMPORTANT NOTES:
// - Wire type between Operations and Vixly Print: add fields at the end, never rename (ADR-0014)
// - Sent once; the code then stops working
// =========================================================
public sealed record PhonePairBody(string Code, string? Device);
