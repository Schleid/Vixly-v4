using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing;

// =========================================================
// FILE: PlatformLicensingAdministration.cs
//
// PURPOSE:
// Shared licensing steps: seats and their limit, the release allowance, recording changes with history, audit and messages (D-L2 to D-L14).
//
// RESPONSIBILITIES:
// - Authorize staff; load a device for the calling tenant only (S-13)
// - Work out the seat limit and whether a release is within the allowance
// - Record a device change: seats, history, audit and the Operations message in one commit
// - Issue secrets and challenges
//
// IMPORTANT NOTES:
// - Platform holds no tenant users: tenant actors are a reference and a name (ADR-0003)
// - Customers are never device-checked (D-L10)
// =========================================================
public sealed class PlatformLicensingAdministration
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformPermissionReader _permissions;

    private readonly IPlatformLicensingRepository _store;

    private readonly IPlatformLicensingQueries _queries;

    private readonly PlatformSubscriptionAdministration _subscriptions;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformTenantRegistrationRepository _registrations;

    private readonly IPlatformPortalSlugRepository _slugs;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformDeviceKeys _keys;

    private readonly IPlatformPublicLinks _links;

    private readonly PlatformAuditTrail _audit;

    private readonly PlatformOutboxWriter _outbox;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly PlatformLegalGate _legal;

    public PlatformLicensingAdministration(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformPermissionReader permissions,
        IPlatformLicensingRepository store,
        IPlatformLicensingQueries queries,
        PlatformSubscriptionAdministration subscriptions,
        IPlatformTenantRepository tenants,
        IPlatformTenantRegistrationRepository registrations,
        IPlatformPortalSlugRepository slugs,
        IPlatformSecretGenerator secrets,
        IPlatformDeviceKeys keys,
        IPlatformPublicLinks links,
        PlatformAuditTrail audit,
        PlatformOutboxWriter outbox,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        PlatformLegalGate legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        _slugs = slugs ?? throw new ArgumentNullException(nameof(slugs));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public IPlatformLicensingRepository Store => _store;

    public IPlatformLicensingQueries Queries => _queries;

    public IPlatformTenantRepository Tenants => _tenants;

    public IPlatformTenantRegistrationRepository Registrations => _registrations;

    public IPlatformPortalSlugRepository Slugs => _slugs;

    public PlatformSubscriptionAdministration Subscriptions => _subscriptions;

    public IPlatformDeviceKeys Keys => _keys;

    public IPlatformPublicLinks Links => _links;

    public IPlatformSecretGenerator Secrets => _secrets;

    public IPlatformUnitOfWork UnitOfWork => _unitOfWork;

    // =====================================================
    // METHOD: STAFF AUTHORIZATION
    // =====================================================
    public async Task<Error?> AuthorizeAsync(PlatformAuthorizationContext context, string permissionCode, bool requireStepUp, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);

        return requireStepUp && !await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken)
            ? PlatformCommonErrors.StepUpRequired
            : null;
    }

    public async Task<bool> HoldsAsync(PlatformAuthorizationContext context, string permissionCode, CancellationToken cancellationToken)
    {
        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(context.PlatformUserId, UtcNow, cancellationToken);
        return actor is not null && actor.IsAccountActive && PlatformAccess.Holds(actor, permissionCode);
    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return true;
        }

        _unitOfWork.DiscardChanges();
        return false;
    }

    // =====================================================
    // METHOD: TENANT CALLS (S-13)
    // =====================================================
    // A device is found only for the tenant the call is for: another tenant's id gives "not found".
    public async Task<PlatformDevice?> DeviceForAsync(PlatformTenantAuthorizationContext caller, Guid deviceId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(caller);
        PlatformDevice? device = await _store.GetDeviceAsync(deviceId, cancellationToken);
        return device is not null && device.PlatformTenantId == caller.PlatformTenantId ? device : null;
    }

    public static PlatformDeviceActor ActorOf(PlatformTenantAuthorizationContext caller)
    {
        ArgumentNullException.ThrowIfNull(caller);
        return PlatformDeviceActor.Tenant(caller.ActorReference, caller.ActorName);
    }

    // =====================================================
    // METHOD: SEATS (D-L2, D-L9)
    // =====================================================
    public async Task<PlatformTenantLicensing> SeatsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        PlatformTenantLicensing? seats = await _store.GetSeatsAsync(tenantId, cancellationToken);

        if (seats is null)
        {
            seats = PlatformTenantLicensing.Create(tenantId, UtcNow);
            _store.AddSeats(seats);
        }

        return seats;
    }

    // Seats bought; while a decrease is scheduled, the lower number counts, so the renewal always fits.
    public static int SeatLimit(PlatformSubscription? subscription)
    {
        if (subscription is null || subscription.Status == PlatformSubscriptionStatus.Ended)
        {
            return 0;
        }

        return subscription.ScheduledChange is { } change ? Math.Min(subscription.Licences, change.TargetLicences) : subscription.Licences;
    }

    // New seats need an active account and full access (not read-only, not payment required).
    public async Task<(PlatformSubscription? Subscription, Error? Refusal)> ReadyForSeatAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        PlatformTenant? tenant = await _tenants.GetAsync(tenantId, cancellationToken);

        if (tenant is null || tenant.Status != PlatformTenantStatus.Active)
        {
            return (null, PlatformLicensingErrors.TenantNotActive);
        }

        PlatformSubscription? subscription = await _subscriptions.Subscriptions.GetCurrentAsync(tenantId, cancellationToken);
        PlatformSubscriptionAccess access = subscription?.AccessAt(UtcNow, await _subscriptions.AccessRulesAsync(cancellationToken)) ?? PlatformSubscriptionAccess.None;

        return access == PlatformSubscriptionAccess.Full ? (subscription, null) : (subscription, PlatformLicensingErrors.AccessNotFull);
    }

    public async Task<PlatformSubscriptionAccess> AccessAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        PlatformSubscription? subscription = await _subscriptions.Subscriptions.GetCurrentAsync(tenantId, cancellationToken);
        PlatformSubscriptionAccess access = subscription?.AccessAt(UtcNow, await _subscriptions.AccessRulesAsync(cancellationToken)) ?? PlatformSubscriptionAccess.None;

        // Terms overdue: read-only until the owner accepts (Slice 8).
        return PlatformLegalGate.Cap(access, await _legal.StateAsync(tenantId, cancellationToken));
    }

    public Task<PlatformTenantLegalState> LegalAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _legal.StateAsync(tenantId, cancellationToken);
    }

    // Owner: 2 releases in any 30 days, or 25% of seats when that is more; over it, Vixly reviews (D-L6).
    public async Task<bool> WithinAllowanceAsync(Guid tenantId, int seats, CancellationToken cancellationToken)
    {
        int used = await _store.CountEndedByTenantSinceAsync(tenantId, UtcNow - PlatformLicensingLimits.AllowanceWindow, cancellationToken);
        return used < PlatformLicensingLimits.AllowanceFor(seats);
    }

    // =====================================================
    // METHOD: RECORD
    // =====================================================
    public void Record(PlatformDevice device, PlatformDeviceChange change, PlatformTenantLicensing seats, int seatLimit, Guid? staffUserId, string actionCode, string? permissionCode)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(change);
        ArgumentNullException.ThrowIfNull(seats);

        seats.Apply(change, seatLimit, UtcNow);
        _store.AddEvents(new[] { change.Event });
        _audit.Record(staffUserId, actionCode, permissionCode, "PlatformDevice", device.Id,
            after: new { number = device.Number, status = device.Status.ToString(), reason = device.Reason?.ToString(), seatHeld = device.SeatHeld, actor = change.Event.ActorName },
            reason: change.Event.Note);

        string? message = device.Status switch
        {
            PlatformDeviceStatus.Active when change.SeatTaken => PlatformMessageTypes.DeviceApproved,
            PlatformDeviceStatus.Released => PlatformMessageTypes.DeviceReleased,
            PlatformDeviceStatus.Revoked when change.Event.EventCode == PlatformDeviceEventCodes.Revoked => PlatformMessageTypes.DeviceRevoked,
            _ => null
        };

        if (message is not null)
        {
            _outbox.Write(message, device.PlatformTenantId, new PlatformDeviceChanged(device.PlatformTenantId, device.Id, device.Number, device.Status, device.Reason, UtcNow));
        }
    }

    public void Audit(Guid? staffUserId, string actionCode, string? permissionCode, string targetType, Guid? targetId, object? after = null, string? reason = null)
    {
        _audit.Record(staffUserId, actionCode, permissionCode, targetType, targetId, after: after, reason: reason);
    }

    public void Notice(Guid tenantId, PlatformLicensingNoticeKind kind, Guid? deviceId = null, Guid? registrationId = null, bool? approved = null, int count = 0,
        Guid? supportRequestId = null, PlatformOwnerSupportKind? supportKind = null)
    {
        _outbox.Write(PlatformMessageTypes.LicensingNotice, tenantId, new PlatformLicensingNotice(tenantId, kind, deviceId, registrationId, approved, count, UtcNow, supportRequestId, supportKind));
    }

    public void RecordTenantActivated(PlatformTenant tenant, PlatformTenantLifecycleEvent change)
    {
        _tenants.AddEvents(new[] { change });
        _audit.Record(null, "platform.tenants.activated_by_registration", null, nameof(PlatformTenant), tenant.Id,
            before: new { status = change.FromStatus?.ToString() }, after: new { status = change.ToStatus.ToString() });
        _outbox.Write(PlatformMessageTypes.TenantStatusChanged, tenant.Id,
            new PlatformTenantStatusChanged(tenant.Id, tenant.TenantCode, change.FromStatus, change.ToStatus, change.Category, change.CreatedUtc));
    }

    // =====================================================
    // METHOD: PROOF MATERIAL (D-L5)
    // =====================================================
    public PlatformSecret NewSecret() => _secrets.NewToken();

    public string Hash(string value) => _secrets.HashToken(value);

    // A link or challenge request counts towards the limit of 3 per hour.
    public async Task<bool> LinkLimitReachedAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _store.CountLinkRequestsSinceAsync(tenantId, UtcNow.AddHours(-1), cancellationToken) >= PlatformLicensingLimits.LinksPerHour;
    }

    public static string ActionFor(string eventCode)
    {
        return eventCode switch
        {
            PlatformDeviceEventCodes.Requested => "platform.licensing.device_requested",
            PlatformDeviceEventCodes.Approved => "platform.licensing.device_approved",
            PlatformDeviceEventCodes.Denied => "platform.licensing.device_denied",
            PlatformDeviceEventCodes.Released => "platform.licensing.device_released",
            PlatformDeviceEventCodes.ReleaseRequested => "platform.licensing.release_requested",
            PlatformDeviceEventCodes.ReleaseApproved or PlatformDeviceEventCodes.ReleaseRefused => "platform.licensing.release_decided",
            PlatformDeviceEventCodes.ReleaseWithdrawn => "platform.licensing.release_withdrawn",
            PlatformDeviceEventCodes.Expired => "platform.licensing.device_expired",
            PlatformDeviceEventCodes.EnrolledByLink => "platform.licensing.device_enrolled_by_link",
            _ => "platform.licensing.device_revoked"
        };
    }
}
