using Vixly.Operations.Domain.Texts;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsTextLog.cs
//
// PURPOSE:
// Keeps a line of the company's text log, and a customer's STOP, on their own (Slice 9; review).
//
// RESPONSIBILITIES:
// - Save the line apart from the caller's work, so neither can undo the other
// - When the customer opted out, take their notices back to email and keep it on the audit trail
//
// IMPORTANT NOTES:
// - Never throws: the text has gone, whatever happens to its log line
// =========================================================
public interface IOperationsTextLog
{
    Task RecordAsync(OperationsTextMessage message, bool optedOut, CancellationToken cancellationToken);
}
