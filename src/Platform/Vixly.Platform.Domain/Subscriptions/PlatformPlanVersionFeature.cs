namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPlanVersionFeature.cs
//
// PURPOSE:
// One feature included in a plan version.
//
// RESPONSIBILITIES:
// - Name the feature
//
// IMPORTANT NOTES:
// - Owned by PlatformPlanVersion; one row per feature
// =========================================================
public sealed class PlatformPlanVersionFeature
{
    private PlatformPlanVersionFeature()
    {
    }

    internal PlatformPlanVersionFeature(PlatformPlanFeature feature)
    {
        Feature = feature;
    }

    public PlatformPlanFeature Feature { get; private set; }
}
