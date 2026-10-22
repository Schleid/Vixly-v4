namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsWaybillParty.cs
//
// PURPOSE:
// A party on a waybill or letter of instruction (Slice 6B step 2, D-6).
//
// RESPONSIBILITIES:
// - Carry the name, the address lines and a phone
//
// IMPORTANT NOTES:
// - Plain text, already joined
// =========================================================
public sealed record OperationsWaybillParty(string Name, IReadOnlyList<string> Lines, string? Phone);
