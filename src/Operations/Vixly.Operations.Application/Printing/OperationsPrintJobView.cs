using Vixly.Operations.Domain.Printing;

namespace Vixly.Operations.Application.Printing;

// =========================================================
// FILE: OperationsPrintJobView.cs
//
// PURPOSE:
// One label handed to the dock print program (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry where to send it and what to send
//
// IMPORTANT NOTES:
// - Address and connection come from the printer record, set by staff in Operations
// =========================================================
public sealed record OperationsPrintJobView(Guid JobId, OperationsPrinterConnection Connection, string Address, string Payload, int ExpiresInSeconds);
