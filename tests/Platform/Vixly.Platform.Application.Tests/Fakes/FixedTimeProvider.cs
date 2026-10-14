namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FixedTimeProvider.cs
//
// PURPOSE:
// A clock that tests set and move.
//
// RESPONSIBILITIES:
// - Return a fixed UTC time; advance on request
//
// IMPORTANT NOTES:
// - Use cases read time only through TimeProvider
// =========================================================
internal sealed class FixedTimeProvider : TimeProvider
{
    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; private set; }

    public override DateTimeOffset GetUtcNow()
    {
        return UtcNow;
    }

    public void Advance(TimeSpan by)
    {
        UtcNow = UtcNow.Add(by);
    }
}
