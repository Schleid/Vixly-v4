namespace Vixly.Contracts.Operations.Printing;

// =========================================================
// FILE: PhonePrinterDto.cs
//
// PURPOSE:
// The answer to a portable printer saved by its phone (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Carry the printer's id in Operations
//
// IMPORTANT NOTES:
// - Wire type between Operations and Vixly Print: add fields at the end, never rename (ADR-0014)
// - The same address of the same phone is the same printer
// =========================================================
public sealed record PhonePrinterDto(Guid PrinterId);
