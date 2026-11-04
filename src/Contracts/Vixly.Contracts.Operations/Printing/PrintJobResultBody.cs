namespace Vixly.Contracts.Operations.Printing;

// =========================================================
// FILE: PrintJobResultBody.cs
//
// PURPOSE:
// What the dock print program reports for a label (Slice 3B).
//
// RESPONSIBILITIES:
// - Say whether it printed, or why not
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// - The error is a short code (letters, digits, underscores)
// =========================================================
public sealed record PrintJobResultBody(bool Printed, string? ErrorCode);
