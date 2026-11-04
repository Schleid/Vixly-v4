using Vixly.Operations.Application.Identity;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsIdleTimesReader.cs
//
// PURPOSE:
// Reads the current company's inactivity times for every signed-in request (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Give the times of the tenant in scope, kept a short while so a request does not read the settings each time
// - Forget them when the company changes them
//
// IMPORTANT NOTES:
// - Kept at most one minute: a change made on another server applies within the minute
// =========================================================
public interface IOperationsIdleTimesReader
{
    Task<OperationsIdleTimes> GetAsync(CancellationToken cancellationToken);

    void Forget();
}
