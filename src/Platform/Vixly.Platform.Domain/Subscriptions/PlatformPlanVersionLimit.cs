namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPlanVersionLimit.cs
//
// PURPOSE:
// One limit of a plan version.
//
// RESPONSIBILITIES:
// - Hold the limit kind and its value
//
// IMPORTANT NOTES:
// - Owned by PlatformPlanVersion; one row per limit kind
// =========================================================
public sealed class PlatformPlanVersionLimit
{
    private PlatformPlanVersionLimit()
    {
    }

    internal PlatformPlanVersionLimit(PlatformPlanLimitKind kind, int value)
    {
        Kind = kind;
        Value = value;
    }

    public PlatformPlanLimitKind Kind { get; private set; }

    public int Value { get; private set; }
}
