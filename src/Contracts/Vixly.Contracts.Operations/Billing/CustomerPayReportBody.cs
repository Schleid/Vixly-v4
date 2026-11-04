namespace Vixly.Contracts.Operations.Billing;

// =========================================================
// FILE: CustomerPayReportBody.cs
//
// PURPOSE:
// The customer says the Zelle or bank transfer is sent (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the reference, the Zelle terms accepted and the form's key
//
// IMPORTANT NOTES:
// - The key makes a second send record nothing more (ADR-0019)
// =========================================================
public sealed record CustomerPayReportBody(string? Reference, bool TermsAccepted, Guid RequestKey);
