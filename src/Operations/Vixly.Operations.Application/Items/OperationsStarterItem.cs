using Vixly.Operations.Domain.Items;

namespace Vixly.Operations.Application.Items;

// =========================================================
// FILE: OperationsStarterItem.cs
//
// PURPOSE:
// One everyday item of the starter catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Carry its description, the words customers use for it, its 6-digit customs code and unit
//
// IMPORTANT NOTES:
// - Copied into a company's catalogue once; the company changes it freely
// =========================================================
public sealed record OperationsStarterItem(string Description, string Matches, string HsCode, OperationsItemUnit Unit);
