using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Printing;

// =========================================================
// FILE: OperationsPrintJob.cs
//
// PURPOSE:
// One label for a dock printer, waiting for the print program (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the printer language (ZPL) to send, and follow it until printed
//
// IMPORTANT NOTES:
// - Printed at most once and only while fresh: a label that comes out late would be stuck on the wrong package, so a lost or failed label is printed again by scanning again
// - The payload is printer commands built by Operations, never text typed by a person
// =========================================================
public sealed class OperationsPrintJob : OperationsEntity
{
    // Long enough for the print program's next calls; short enough that the clerk is still at the same package.
    public static readonly TimeSpan FreshFor = TimeSpan.FromMinutes(2);

    private OperationsPrintJob()
    {
        Payload = string.Empty;
    }

    private OperationsPrintJob(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Payload = string.Empty;
    }

    public Guid PrinterId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public string Payload { get; private set; }

    public OperationsPrintJobStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? TakenUtc { get; private set; }

    public DateTimeOffset? FinishedUtc { get; private set; }

    public string? ErrorCode { get; private set; }

    public static OperationsPrintJob Queue(Guid tenantId, OperationsLabelPrinter printer, string payload, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        return new(tenantId, utcNow) { PrinterId = printer.Id, WarehouseId = printer.WarehouseId, Payload = payload, Status = OperationsPrintJobStatus.Pending };
    }

    public bool CanBeTaken(DateTimeOffset utcNow) => Status == OperationsPrintJobStatus.Pending && CreatedUtc + FreshFor > utcNow;

    public void Take(DateTimeOffset utcNow)
    {
        (Status, TakenUtc, Attempts) = (OperationsPrintJobStatus.Taken, utcNow, Attempts + 1);
        MarkAsModified(utcNow);
    }

    public void Finish(bool printed, string? errorCode, DateTimeOffset utcNow)
    {
        Status = printed ? OperationsPrintJobStatus.Printed : OperationsPrintJobStatus.Failed;
        ErrorCode = printed ? null : errorCode is { Length: > 0 } code ? code[..Math.Min(code.Length, 60)] : "print_failed";
        FinishedUtc = printed ? utcNow : null;
        MarkAsModified(utcNow);
    }
}
