namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCompanyChange.cs
//
// PURPOSE:
// The latest request to change the legal details, and how it went (B12).
//
// RESPONSIBILITIES:
// - Carry what Platform answered
//
// IMPORTANT NOTES:
// - Read-only; mapped from Contracts.Platform in Infrastructure
// =========================================================
public sealed record OperationsCompanyChange(Guid RequestId, string Status, OperationsCompanyDetails Proposed, string Reason, string SubmittedByName, DateTimeOffset SubmittedUtc, DateTimeOffset? DecidedUtc, string? DecisionNote);
