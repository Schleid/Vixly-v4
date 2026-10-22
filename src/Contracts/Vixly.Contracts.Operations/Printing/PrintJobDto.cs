namespace Vixly.Contracts.Operations.Printing;

// =========================================================
// FILE: PrintJobDto.cs
//
// PURPOSE:
// One label for the dock print program (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry where to send it and the printer commands
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// - Connection is "network" (host:port) or "installed" (a Windows printer name)
// - ExpiresInSeconds: after that long the label is not printed any more (a late label goes on the wrong package)
// =========================================================
public sealed record PrintJobDto(Guid JobId, string Connection, string Address, string Payload, int ExpiresInSeconds);
