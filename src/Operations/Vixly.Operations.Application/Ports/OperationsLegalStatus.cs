namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsLegalStatus.cs
//
// PURPOSE:
// The tenant's Terms status and the version to accept (Platform Slice 8).
//
// RESPONSIBILITIES:
// - Carry status, deadline, read-only and the Terms shown to the Owner
//
// IMPORTANT NOTES:
// - Status codes: not_required, accepted, due, overdue
// =========================================================
public sealed record OperationsLegalStatus(string Status, DateTimeOffset? DeadlineUtc, bool ReadOnly, OperationsLegalTerms? Terms);
