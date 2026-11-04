namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsSuspensionLift.cs
//
// PURPOSE:
// Lifts the customer suspensions whose end day has come, in every company (owner, 2026-10-31).
//
// RESPONSIBILITIES:
// - Make each such customer active again, with the reason recorded
//
// IMPORTANT NOTES:
// - Run by the housekeeping job in the system scope; returns how many were lifted
// =========================================================
public interface IOperationsSuspensionLift
{
    Task<int> RunAsync(int batchSize, DateTimeOffset utcNow, CancellationToken cancellationToken);
}
