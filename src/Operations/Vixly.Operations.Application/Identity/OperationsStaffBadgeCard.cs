namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsStaffBadgeCard.cs
//
// PURPOSE:
// What a staff badge card shows (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Carry the company's name, the person's name and the badge's code
//
// IMPORTANT NOTES:
// - The company's own name, never "Vixly" (ADR-0012 W-1)
// =========================================================
public sealed record OperationsStaffBadgeCard(string CompanyName, string PersonName, string Code);
