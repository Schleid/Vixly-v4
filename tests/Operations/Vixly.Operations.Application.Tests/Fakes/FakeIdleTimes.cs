using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeIdleTimes.cs
//
// PURPOSE:
// The company's inactivity times for use case tests (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Give the default times; count Forget calls
//
// IMPORTANT NOTES:
// - No database, no caching
// =========================================================
internal sealed class FakeIdleTimes : IOperationsIdleTimesReader
{
    public int Forgotten { get; private set; }

    public Task<OperationsIdleTimes> GetAsync(CancellationToken cancellationToken) => Task.FromResult(OperationsIdleTimes.Default);

    public void Forget() => Forgotten++;
}
