using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsVersions.cs
//
// PURPOSE:
// The rules of dated versions, shared by price lists, extras and the insurance plan (Slice 4B design section 5).
//
// RESPONSIBILITIES:
// - Find the version in force at a moment
// - Schedule a version, replacing a scheduled one that starts at the same moment
// - Remove a scheduled version
//
// IMPORTANT NOTES:
// - A version that has started never changes: it can only be followed by a newer one
// - A start in the past becomes "now"; more than a day in the past is refused
// =========================================================
public static class OperationsVersions
{
    private static readonly TimeSpan PastTolerance = TimeSpan.FromDays(1);

    public static T? At<T>(IEnumerable<T> versions, DateTimeOffset at)
        where T : class, IOperationsVersion =>
        versions.Where(version => version.EffectiveFromUtc <= at).OrderByDescending(version => version.EffectiveFromUtc).FirstOrDefault();

    // The moment a new version starts: now at the earliest.
    public static DateTimeOffset Start(DateTimeOffset? requested, DateTimeOffset utcNow) =>
        requested is not { } start || start <= utcNow
            ? (requested is { } past && past < utcNow - PastTolerance ? throw new DomainException(OperationsChargeErrors.EffectiveInvalid) : utcNow)
            : start;

    public static void Schedule<T>(List<T> versions, T version, DateTimeOffset utcNow)
        where T : class, IOperationsVersion
    {
        ArgumentNullException.ThrowIfNull(versions);
        ArgumentNullException.ThrowIfNull(version);

        if (version.EffectiveFromUtc < utcNow - TimeSpan.FromMinutes(1))
        {
            throw new DomainException(OperationsChargeErrors.EffectiveInvalid);
        }

        versions.RemoveAll(other => other.EffectiveFromUtc > utcNow && other.EffectiveFromUtc == version.EffectiveFromUtc);

        if (versions.Count >= OperationsChargeLimits.MaxVersions)
        {
            throw new DomainException(OperationsChargeErrors.TooMany);
        }

        versions.Add(version);
        versions.Sort((left, right) => left.EffectiveFromUtc.CompareTo(right.EffectiveFromUtc));
    }

    public static void Remove<T>(List<T> versions, Guid versionId, DateTimeOffset utcNow)
        where T : class, IOperationsVersion
    {
        ArgumentNullException.ThrowIfNull(versions);
        T version = versions.FirstOrDefault(item => item.Id == versionId) ?? throw new DomainException(OperationsChargeErrors.VersionNotFound);

        if (version.EffectiveFromUtc <= utcNow)
        {
            throw new DomainException(OperationsChargeErrors.VersionStarted);
        }

        // An owner keeps at least one version; checked before the list changes.
        if (versions.Count == 1)
        {
            throw new DomainException(OperationsChargeErrors.VersionStarted);
        }

        versions.Remove(version);
    }
}
