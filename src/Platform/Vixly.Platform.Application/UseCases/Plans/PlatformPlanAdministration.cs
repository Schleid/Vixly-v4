using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans;

// =========================================================
// FILE: PlatformPlanAdministration.cs
//
// PURPOSE:
// The steps every plan command shares.
//
// RESPONSIBILITIES:
// - Check the permission and, when asked, step-up; load the plan
// - Read the reader abilities
// - Write the audit entry
//
// IMPORTANT NOTES:
// - One instance per scope, like the use cases
// =========================================================
public sealed class PlatformPlanAdministration
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformPermissionReader _permissions;

    private readonly IPlatformPlanRepository _plans;

    private readonly PlatformAuditTrail _audit;

    private readonly TimeProvider _timeProvider;

    public PlatformPlanAdministration(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformPermissionReader permissions,
        IPlatformPlanRepository plans,
        PlatformAuditTrail audit,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public async Task<Error?> AuthorizeAsync(
        PlatformAuthorizationContext context,
        string permissionCode,
        bool requireStepUp,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);

        return requireStepUp && !await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken)
            ? PlatformCommonErrors.StepUpRequired
            : null;
    }

    public async Task<PlatformPlanTarget> LoadPlanAsync(
        PlatformAuthorizationContext context,
        string permissionCode,
        Guid platformPlanId,
        bool requireStepUp,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);

        if (requireStepUp && !await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken))
        {
            return PlatformPlanTarget.Refused(PlatformCommonErrors.StepUpRequired);
        }

        PlatformPlan? plan = await _plans.GetAsync(platformPlanId, cancellationToken);

        return plan is null ? PlatformPlanTarget.Refused(PlatformCommonErrors.NotFound) : PlatformPlanTarget.Found(plan);
    }

    public async Task<PlatformPlanAbilities> ReadAbilitiesAsync(PlatformAuthorizationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(context.PlatformUserId, UtcNow, cancellationToken);

        return actor is null || !actor.IsAccountActive
            ? PlatformPlanAbilities.None
            : new PlatformPlanAbilities(
                PlatformAccess.Holds(actor, PlatformPermissions.PlansManage),
                PlatformAccess.Holds(actor, PlatformPermissions.PlansPublish));
    }

    public void RecordChange(
        PlatformAuthorizationContext context,
        PlatformPlan plan,
        string permissionCode,
        string actionCode,
        object? before = null,
        object? after = null,
        string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(plan);

        _audit.Record(context.PlatformUserId, actionCode, permissionCode, nameof(PlatformPlan), plan.Id, before: before, after: after, reason: reason);
    }
}
