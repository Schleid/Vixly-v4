using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.Departments;

// =========================================================
// FILE: PlatformDepartmentAdministration.cs
//
// PURPOSE:
// The checks every department action runs before it changes
// anything.
//
// RESPONSIBILITIES:
// - Authorize the permission (S-1)
// - Load the department, or answer NotFound
// - Read the actor's access fresh, to offer actions
//
// IMPORTANT NOTES:
// - No step-up: departments are labels and grant nothing
//   (decision D-S3)
// =========================================================
public sealed class PlatformDepartmentAdministration
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformPermissionReader _permissions;

    private readonly IPlatformDepartmentRepository _departments;

    private readonly TimeProvider _timeProvider;

    public PlatformDepartmentAdministration(
        IPlatformAuthorizationGate gate,
        IPlatformPermissionReader permissions,
        IPlatformDepartmentRepository departments,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _departments = departments ?? throw new ArgumentNullException(nameof(departments));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PlatformDepartmentTarget> LoadDepartmentAsync(
        PlatformAuthorizationContext context,
        string permissionCode,
        Guid platformDepartmentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);

        PlatformDepartment? department = await _departments.GetAsync(platformDepartmentId, cancellationToken);

        return department is null
            ? PlatformDepartmentTarget.Refused(PlatformCommonErrors.NotFound)
            : PlatformDepartmentTarget.Found(department);
    }

    public Task<PlatformPermissionSnapshot?> ReadActorAsync(
        PlatformAuthorizationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _permissions.ReadAsync(context.PlatformUserId, _timeProvider.GetUtcNow(), cancellationToken);
    }
}
