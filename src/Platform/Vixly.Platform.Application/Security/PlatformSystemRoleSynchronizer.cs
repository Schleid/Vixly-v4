using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformSystemRoleSynchronizer.cs
//
// PURPOSE:
// Brings the system roles in the database in line with the
// definitions in code, at every start.
//
// RESPONSIBILITIES:
// - Create a missing system role with its permissions
// - Correct the permissions of a system role that drifted
// - Audit each change with no actor (the deploy did it)
//
// IMPORTANT NOTES:
// - Idempotent: a second run changes nothing
// - A custom role already using a system key as its name is left
//   alone and reported as a conflict, never renamed
// - Replaces V3's synchronisation service and its repair panel
// =========================================================
public sealed class PlatformSystemRoleSynchronizer
{
    private readonly IPlatformRoleRepository _roles;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlatformSystemRoleSynchronizer(
        IPlatformRoleRepository roles,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PlatformSystemRoleSyncResult> SynchronizeAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        IReadOnlyList<PlatformRole> existing = await _roles.ListSystemAsync(cancellationToken);
        List<string> created = new();
        List<string> corrected = new();
        List<string> conflicts = new();

        foreach ((string systemKey, IReadOnlyCollection<string> codes) in PlatformSystemRoles.Definitions)
        {
            PlatformRole? role = existing.FirstOrDefault(candidate => candidate.SystemKey == systemKey);

            if (role is null)
            {
                if (await _roles.NameExistsAsync(systemKey, null, cancellationToken))
                {
                    conflicts.Add(systemKey);
                    continue;
                }

                role = PlatformRole.CreateSystem(systemKey, utcNow);
                role.SynchronizeSystemPermissions(codes, utcNow);
                _roles.Add(role);
                created.Add(systemKey);
                _audit.Record(null, "platform.roles.system_created", null, nameof(PlatformRole), role.Id, after: Sorted(codes));
                continue;
            }

            List<string> before = Sorted(role.Permissions.Select(permission => permission.PermissionCode));

            if (before.SequenceEqual(Sorted(codes), StringComparer.Ordinal))
            {
                continue;
            }

            role.SynchronizeSystemPermissions(codes, utcNow);
            corrected.Add(systemKey);
            _audit.Record(null, "platform.roles.system_corrected", null, nameof(PlatformRole), role.Id, before: before, after: Sorted(codes));
        }

        if (created.Count + corrected.Count > 0 && !await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return new PlatformSystemRoleSyncResult(Array.Empty<string>(), Array.Empty<string>(), conflicts, Saved: false);
        }

        return new PlatformSystemRoleSyncResult(created, corrected, conflicts, Saved: true);
    }

    private static List<string> Sorted(IEnumerable<string> codes)
    {
        return codes.Distinct(StringComparer.Ordinal).OrderBy(code => code, StringComparer.Ordinal).ToList();
    }
}
