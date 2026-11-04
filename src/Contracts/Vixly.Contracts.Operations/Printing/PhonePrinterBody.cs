namespace Vixly.Contracts.Operations.Printing;

// =========================================================
// FILE: PhonePrinterBody.cs
//
// PURPOSE:
// A portable printer next to a phone running Vixly Print (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Carry its name, the phone's identifier of it, its resolution, its labels ("id" or "shipping") and whether it is in use
//
// IMPORTANT NOTES:
// - Wire type between Operations and Vixly Print: add fields at the end, never rename (ADR-0014)
// - Sent with the print program key
// =========================================================
public sealed record PhonePrinterBody(string Name, string Address, int Dpi, string Media, bool Active);
