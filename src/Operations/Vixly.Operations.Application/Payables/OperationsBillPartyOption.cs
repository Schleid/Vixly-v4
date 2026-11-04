using Vixly.Operations.Domain.Payables;

namespace Vixly.Operations.Application.Payables;

// =========================================================
// FILE: OperationsBillPartyOption.cs
//
// PURPOSE:
// Who can send the company a bill, for the bill form (Slice 8): its carriers, partner agents and other vendors.
//
// RESPONSIBILITIES:
// - Carry the kind, id, name and the usual category
//
// IMPORTANT NOTES:
// - Read-only
// =========================================================
public sealed record OperationsBillPartyOption(OperationsBillParty Party, Guid Id, string Name, OperationsBillCategory Category);
