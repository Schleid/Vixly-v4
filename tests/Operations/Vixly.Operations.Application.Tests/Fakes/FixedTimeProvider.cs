namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FixedTimeProvider.cs
//
// PURPOSE:
// A clock that only moves when told.
//
// RESPONSIBILITIES:
// - Return a fixed time
//
// IMPORTANT NOTES:
// - Tests only
// =========================================================
internal sealed class FixedTimeProvider : TimeProvider
{
    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; private set; }

    public override DateTimeOffset GetUtcNow() => UtcNow;

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
