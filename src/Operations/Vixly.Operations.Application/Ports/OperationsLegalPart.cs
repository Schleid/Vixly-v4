namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsLegalPart.cs
//
// PURPOSE:
// A document the Terms include (Privacy, DPA, ...).
//
// RESPONSIBILITIES:
// - Carry the kind, version and summary
//
// IMPORTANT NOTES:
// - Read-only; accepted with the Terms
// =========================================================
public sealed record OperationsLegalPart(Guid DocumentId, string Kind, string Version, string? Summary);
