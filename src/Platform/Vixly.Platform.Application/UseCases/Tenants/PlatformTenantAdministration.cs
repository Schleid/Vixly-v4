using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.Tenants;

// =========================================================
// FILE: PlatformTenantAdministration.cs
//
// PURPOSE:
// Shared steps of the tenant use cases.
//
// RESPONSIBILITIES:
// - Check the permission (and step-up) and load the tenant
// - Work out what the actor may do
// - Record a lifecycle move: history event, audit entry and
//   outbox message in one unit of work (D-2, D-T17)
// - Generate a unique tenant code (D-T13)
//
// IMPORTANT NOTES:
// - Saving stays with the use case
// - Ban needs SuperUser authority as well as the permission
// =========================================================
public sealed class PlatformTenantAdministration
{
    private const int CodeAttempts = 10;

    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformPermissionReader _permissions;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformRandom _random;

    private readonly PlatformAuditTrail _audit;

    private readonly PlatformOutboxWriter _outbox;

    private readonly TimeProvider _timeProvider;

    public PlatformTenantAdministration(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformPermissionReader permissions,
        IPlatformTenantRepository tenants,
        IPlatformRandom random,
        PlatformAuditTrail audit,
        PlatformOutboxWriter outbox,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public async Task<PlatformTenantTarget> LoadTenantAsync(
        PlatformAuthorizationContext context,
        string permissionCode,
        Guid platformTenantId,
        bool requireStepUp,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);

        if (requireStepUp && !await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken))
        {
            return PlatformTenantTarget.Refused(PlatformCommonErrors.StepUpRequired);
        }

        PlatformTenant? tenant = await _tenants.GetAsync(platformTenantId, cancellationToken);

        return tenant is null
            ? PlatformTenantTarget.Refused(PlatformCommonErrors.NotFound)
            : PlatformTenantTarget.Found(tenant);
    }

    public async Task<PlatformTenantAbilities> ReadAbilitiesAsync(
        PlatformAuthorizationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(context.PlatformUserId, UtcNow, cancellationToken);

        if (actor is null || !actor.IsAccountActive)
        {
            return PlatformTenantAbilities.None;
        }

        return new PlatformTenantAbilities(
            PlatformAccess.Holds(actor, PlatformPermissions.TenantsEdit),
            PlatformAccess.Holds(actor, PlatformPermissions.TenantsAddNotes),
            PlatformAccess.Holds(actor, PlatformPermissions.TenantsActivate),
            PlatformAccess.Holds(actor, PlatformPermissions.TenantsSuspend),
            PlatformAccess.Holds(actor, PlatformPermissions.TenantsReactivate),
            PlatformAccess.Holds(actor, PlatformPermissions.TenantsDeactivate),
            PlatformAccess.Holds(actor, PlatformPermissions.TenantsBan)
                && actor.AuthorityLevel == PlatformUserAuthorityLevel.SuperUser);
    }

    public void RecordMove(
        PlatformAuthorizationContext context,
        PlatformTenant tenant,
        PlatformTenantLifecycleEvent change,
        string permissionCode,
        string actionCode)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(change);

        _tenants.AddEvents(new[] { change });
        _audit.Record(
            context.PlatformUserId,
            actionCode,
            permissionCode,
            nameof(PlatformTenant),
            tenant.Id,
            before: new { status = change.FromStatus?.ToString() },
            after: new { status = change.ToStatus.ToString(), category = change.Category?.ToString() },
            reason: change.Reason);
        _outbox.Write(
            PlatformMessageTypes.TenantStatusChanged,
            tenant.Id,
            new PlatformTenantStatusChanged(tenant.Id, tenant.TenantCode, change.FromStatus, change.ToStatus, change.Category, change.CreatedUtc));
    }

    public void RecordChange(
        PlatformAuthorizationContext context,
        PlatformTenant tenant,
        string permissionCode,
        string actionCode,
        object? before = null,
        object? after = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenant);

        _audit.Record(context.PlatformUserId, actionCode, permissionCode, nameof(PlatformTenant), tenant.Id, before: before, after: after);
    }

    public async Task<string?> NewTenantCodeAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[PlatformTenantCodeFormat.RandomByteCount];

        for (int attempt = 0; attempt < CodeAttempts; attempt++)
        {
            _random.Fill(buffer);
            string code = PlatformTenantCodeFormat.FromRandom(buffer);

            if (!await _tenants.CodeExistsAsync(code, cancellationToken))
            {
                return code;
            }
        }

        return null;
    }
}
