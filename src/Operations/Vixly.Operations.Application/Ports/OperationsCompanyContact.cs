namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCompanyContact.cs
//
// PURPOSE:
// A company contact at Vixly: owner, billing or technical.
//
// RESPONSIBILITIES:
// - Carry what Platform answered
//
// IMPORTANT NOTES:
// - Read-only; mapped from Contracts.Platform in Infrastructure
// =========================================================
public sealed record OperationsCompanyContact(string Role, string Name, string Email, string? Phone);
