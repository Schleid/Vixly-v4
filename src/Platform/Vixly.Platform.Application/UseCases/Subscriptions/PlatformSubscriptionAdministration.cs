using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionAdministration.cs
//
// PURPOSE:
// The steps every subscription command shares.
//
// RESPONSIBILITIES:
// - Check the permission and, when asked, step-up; load the current subscription and its plan
// - Work out the licence ceiling (plan or override) and the licences in use
// - Read what the reader may do
// - Write the history event, the audit entry and the outbox message in the same unit of work (D-2, D-S11)
//
// IMPORTANT NOTES:
// - Commands from the console pass their context; the clock, the handlers and registration pass none
// - Messages are written on the tenant stream so they stay in order with tenant status messages
// =========================================================
public sealed class PlatformSubscriptionAdministration
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformPermissionReader _permissions;

    private readonly IPlatformSubscriptionRepository _subscriptions;

    private readonly IPlatformPlanRepository _plans;

    private readonly IPlatformLimitOverrideRepository _overrides;

    private readonly IPlatformLicenceCounter _licences;

    private readonly PlatformAuditTrail _audit;

    private readonly PlatformOutboxWriter _outbox;

    private readonly IPlatformCollectionsRepository _collections;

    private readonly TimeProvider _timeProvider;

    public PlatformSubscriptionAdministration(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformPermissionReader permissions,
        IPlatformSubscriptionRepository subscriptions,
        IPlatformPlanRepository plans,
        IPlatformLimitOverrideRepository overrides,
        IPlatformLicenceCounter licences,
        PlatformAuditTrail audit,
        PlatformOutboxWriter outbox,
        IPlatformCollectionsRepository collections,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _overrides = overrides ?? throw new ArgumentNullException(nameof(overrides));
        _licences = licences ?? throw new ArgumentNullException(nameof(licences));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public IPlatformSubscriptionRepository Subscriptions => _subscriptions;

    public IPlatformPlanRepository Plans => _plans;

    public IPlatformLimitOverrideRepository Overrides => _overrides;

    public PlatformOutboxWriter Outbox => _outbox;

    // Read-only periods come from Collections settings (Slice 6, D-C4, D-C5); defaults until the row exists.
    public async Task<PlatformSubscriptionAccessRules> AccessRulesAsync(CancellationToken cancellationToken)
    {
        return ((await _collections.FindSettingsAsync(cancellationToken)) ?? Vixly.Platform.Domain.Collections.PlatformCollectionsSettings.Create(UtcNow)).AccessRules;
    }

    public static LanguageCode Language(string? value)
    {
        return PlatformLanguageInput.Read(value);
    }

    // =====================================================
    // METHOD: AUTHORIZE AND LOAD
    // =====================================================
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

    public async Task<PlatformSubscriptionTarget> LoadAsync(
        PlatformAuthorizationContext context,
        string permissionCode,
        Guid platformTenantId,
        bool requireStepUp,
        CancellationToken cancellationToken)
    {
        Error? refused = await AuthorizeAsync(context, permissionCode, requireStepUp, cancellationToken);

        if (refused is not null)
        {
            return PlatformSubscriptionTarget.Refused(refused);
        }

        PlatformSubscription? subscription = await _subscriptions.GetCurrentAsync(platformTenantId, cancellationToken);

        if (subscription is null)
        {
            return PlatformSubscriptionTarget.Refused(PlatformSubscriptionErrors.NoSubscription);
        }

        PlatformPlan? plan = await _plans.GetAsync(subscription.PlatformPlanId, cancellationToken);

        return plan is null
            ? PlatformSubscriptionTarget.Refused(PlatformCommonErrors.NotFound)
            : PlatformSubscriptionTarget.Found(subscription, plan);
    }

    // =====================================================
    // METHOD: LIMITS AND LICENCES (D-S5, D-S8)
    // =====================================================
    public async Task<int> MaxLicencesAsync(Guid platformTenantId, PlatformPlanVersion version, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(version);

        IReadOnlyList<PlatformTenantLimitOverride> open = await _overrides.ListOpenAsync(platformTenantId, cancellationToken);
        PlatformTenantLimitOverride? devices = open.FirstOrDefault(item =>
            item.Kind == PlatformPlanLimitKind.MaxLicensedDevices && item.AppliesAt(UtcNow));

        return devices?.Value ?? version.Limit(PlatformPlanLimitKind.MaxLicensedDevices);
    }

    public Task<int> ActiveLicencesAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return _licences.CountActiveAsync(platformTenantId, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformLimitItem>> LimitsAsync(
        Guid platformTenantId,
        PlatformPlanVersion? version,
        IReadOnlyDictionary<Guid, string>? staffNames,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PlatformTenantLimitOverride> open = await _overrides.ListOpenAsync(platformTenantId, cancellationToken);
        DateTimeOffset utcNow = UtcNow;

        return Enum.GetValues<PlatformPlanLimitKind>()
            .Select(kind =>
            {
                int planValue = version?.Limit(kind) ?? 0;
                PlatformTenantLimitOverride? current = open.FirstOrDefault(item => item.Kind == kind && item.AppliesAt(utcNow));
                string? setBy = current is not null && staffNames is not null && staffNames.TryGetValue(current.SetByUserId, out string? name) ? name : null;

                return new PlatformLimitItem(
                    kind,
                    planValue,
                    current?.Value ?? planValue,
                    current?.Id,
                    current?.Value,
                    current?.Reason,
                    current?.EndsUtc,
                    setBy);
            })
            .ToArray();
    }

    public async Task<IReadOnlyList<PlatformPlanOption>> PlanOptionsAsync(
        Guid platformTenantId,
        LanguageCode language,
        CancellationToken cancellationToken,
        bool publicOnly = false)
    {
        IReadOnlyList<PlatformPlan> plans = await _plans.ListChoosableAsync(publicOnly, cancellationToken);
        List<PlatformPlanOption> options = new();

        foreach (PlatformPlan plan in plans)
        {
            options.Add(await OptionAsync(platformTenantId, plan, language, cancellationToken));
        }

        return options;
    }

    public async Task<PlatformPlanOption> OptionAsync(Guid platformTenantId, PlatformPlan plan, LanguageCode language, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);

        PlatformPlanVersion version = plan.Current ?? throw new InvalidOperationException("The plan has no current version.");

        return new PlatformPlanOption(
            plan.Id,
            plan.Code,
            plan.NameIn(language),
            version.CurrencyCode,
            version.MonthlyPricePerLicence,
            version.AnnualPricePerLicence,
            version.SetupFee,
            version.MinimumLicences,
            await MaxLicencesAsync(platformTenantId, version, cancellationToken),
            version.TrialDays);
    }

    // =====================================================
    // METHOD: ABILITIES
    // =====================================================
    public async Task<PlatformSubscriptionAbilities> ReadAbilitiesAsync(
        PlatformAuthorizationContext context,
        PlatformSubscription? subscription,
        bool tenantCanSubscribe,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(context.PlatformUserId, UtcNow, cancellationToken);

        if (actor is null || !actor.IsAccountActive)
        {
            return PlatformSubscriptionAbilities.None;
        }

        bool manage = PlatformAccess.Holds(actor, PlatformPermissions.SubscriptionsManage);
        bool overrides = PlatformAccess.Holds(actor, PlatformPermissions.SubscriptionsOverrideLimits);
        bool grant = PlatformAccess.Holds(actor, PlatformPermissions.DiscountsGrant);
        bool decide = PlatformAccess.Holds(actor, PlatformPermissions.DiscountsApprove);
        bool charges = PlatformAccess.Holds(actor, PlatformPermissions.SubscriptionsCharges);

        if (subscription is null)
        {
            return PlatformSubscriptionAbilities.None with { CanStart = manage && tenantCanSubscribe, CanOverrideLimits = overrides };
        }

        PlatformSubscriptionStatus status = subscription.Status;
        bool changeable = status is not (PlatformSubscriptionStatus.Ended or PlatformSubscriptionStatus.Suspended);
        bool paid = status is PlatformSubscriptionStatus.Active or PlatformSubscriptionStatus.PastDue;

        return new PlatformSubscriptionAbilities(
            CanStart: false,
            CanChange: manage && changeable && !subscription.CancelAtPeriodEnd,
            CanExtendTrial: manage && status == PlatformSubscriptionStatus.Trialing,
            CanEndTrial: manage && status == PlatformSubscriptionStatus.Trialing,
            CanRecordPayment: false, // Slice 4: payments are recorded on the invoice (D-B7); paying it activates the subscription
            CanCancel: manage && changeable && !subscription.CancelAtPeriodEnd,
            CanResume: manage && paid && subscription.CancelAtPeriodEnd,
            CanEndNow: manage && status != PlatformSubscriptionStatus.Ended,
            CanCancelScheduledChange: manage && subscription.ScheduledChange is not null,
            CanOverrideLimits: overrides,
            CanGrantDiscount: grant && changeable,
            CanDecideDiscount: decide && subscription.PendingDiscount is { } pending && pending.GrantedByUserId != context.PlatformUserId,
            CanManageCharges: charges && status != PlatformSubscriptionStatus.Ended);
    }

    // =====================================================
    // METHOD: RECORD (D-2, D-S11)
    // =====================================================
    public void RecordChange(
        PlatformAuthorizationContext? context,
        PlatformSubscription subscription,
        PlatformPlan plan,
        PlatformSubscriptionEvent change,
        string? permissionCode,
        string actionCode,
        decimal chargeNow = 0m,
        PlatformTenantAuthorizationContext? tenantActor = null)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(change);

        _subscriptions.AddEvents(new[] { change });
        _audit.Record(
            context?.PlatformUserId,
            actionCode,
            permissionCode,
            nameof(PlatformSubscription),
            subscription.Id,
            arguments: change.Arguments,
            before: new { status = change.FromStatus?.ToString() },
            after: tenantActor is null
                ? new { status = change.ToStatus.ToString(), plan = plan.Code, licences = subscription.Licences, term = subscription.Term.ToString() }
                : (object)new { status = change.ToStatus.ToString(), plan = plan.Code, licences = subscription.Licences, term = subscription.Term.ToString(), actor = tenantActor.ActorReference, actorName = tenantActor.ActorName, role = PlatformTenantAccess.RoleName(tenantActor.Role) },
            reason: change.Reason);
        _outbox.Write(MessageTypeFor(change.EventCode), subscription.PlatformTenantId, Message(subscription, plan, change, chargeNow));
    }

    public void RecordChange(
        PlatformAuthorizationContext context,
        PlatformTenantLimitOverride limitOverride,
        string permissionCode,
        string actionCode)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(limitOverride);

        _audit.Record(
            context.PlatformUserId,
            actionCode,
            permissionCode,
            "PlatformTenant",
            limitOverride.PlatformTenantId,
            after: new { limit = limitOverride.Kind.ToString(), value = limitOverride.Value, ends = limitOverride.EndsUtc, removed = !limitOverride.IsOpen },
            reason: limitOverride.Reason);
    }

    public static PlatformSubscriptionMessage Message(
        PlatformSubscription subscription,
        PlatformPlan plan,
        PlatformSubscriptionEvent change,
        decimal chargeNow)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(change);

        PlatformPlanVersion? version = plan.Versions.FirstOrDefault(item => item.Id == subscription.PlanVersionId);
        decimal periodAmount = version is null ? 0m : PlatformPricing.PeriodAmount(version, subscription.Term, subscription.Licences);
        decimal setupFee = version is not null && change.Arguments.TryGetValue("setupFee", out string? charged) && charged == "true"
            ? subscription.SetupFeeFor(version)
            : 0m;
        bool detailed = change.EventCode is PlatformSubscriptionEventCodes.ChargeAdded or PlatformSubscriptionEventCodes.ChargeCancelled
            or PlatformSubscriptionEventCodes.SetupFeeChanged or PlatformSubscriptionEventCodes.DiscountApplied
            or PlatformSubscriptionEventCodes.DiscountApproved or PlatformSubscriptionEventCodes.DiscountEnded;

        return new PlatformSubscriptionMessage(
            subscription.Id,
            subscription.PlatformTenantId,
            change.EventCode,
            subscription.Status,
            subscription.PlatformPlanId,
            subscription.PlanVersionId,
            subscription.Term,
            subscription.Licences,
            chargeNow,
            setupFee,
            periodAmount,
            version?.CurrencyCode ?? plan.CurrencyCode,
            subscription.PeriodStartUtc,
            subscription.PeriodEndUtc,
            change.CreatedUtc,
            subscription.CurrentPeriodOff(periodAmount),
            detailed ? change.Arguments : null);
    }

    public static string MessageTypeFor(string eventCode)
    {
        return eventCode switch
        {
            PlatformSubscriptionEventCodes.StartedTrial or PlatformSubscriptionEventCodes.StartedAwaitingPayment => PlatformMessageTypes.SubscriptionStarted,
            PlatformSubscriptionEventCodes.Changed => PlatformMessageTypes.SubscriptionChanged,
            PlatformSubscriptionEventCodes.ChangeScheduled or PlatformSubscriptionEventCodes.ScheduledChangeCancelled => PlatformMessageTypes.SubscriptionChangeScheduled,
            PlatformSubscriptionEventCodes.Renewed => PlatformMessageTypes.SubscriptionRenewed,
            PlatformSubscriptionEventCodes.CancellationRequested or PlatformSubscriptionEventCodes.Resumed => PlatformMessageTypes.SubscriptionCancellationRequested,
            PlatformSubscriptionEventCodes.TrialExtended => PlatformMessageTypes.SubscriptionTrialExtended,
            PlatformSubscriptionEventCodes.Ended => PlatformMessageTypes.SubscriptionEnded,
            PlatformSubscriptionEventCodes.DiscountRequested or PlatformSubscriptionEventCodes.DiscountApplied or PlatformSubscriptionEventCodes.DiscountApproved
                or PlatformSubscriptionEventCodes.DiscountRejected or PlatformSubscriptionEventCodes.DiscountEnded => PlatformMessageTypes.SubscriptionDiscountChanged,
            PlatformSubscriptionEventCodes.ChargeAdded or PlatformSubscriptionEventCodes.ChargeCancelled
                or PlatformSubscriptionEventCodes.SetupFeeChanged => PlatformMessageTypes.SubscriptionChargeChanged,
            _ => PlatformMessageTypes.SubscriptionStatusChanged
        };
    }
}
