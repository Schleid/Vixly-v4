namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsTrackingNumberBackfill.cs
//
// PURPOSE:
// Gives a tracking number to booked shipments that are paid, charged to an account or collect and still have none (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Shipments booked before tracking numbers existed, and any a save missed
//
// IMPORTANT NOTES:
// - Runs in the system scope from the housekeeping round; saves its own batch
// =========================================================
public interface IOperationsTrackingNumberBackfill
{
    Task<int> RunAsync(int batchSize, DateTimeOffset utcNow, CancellationToken cancellationToken);
}
