using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.VerifyOperationsDevice;

// =========================================================
// FILE: VerifyOperationsDeviceUseCase.cs
//
// PURPOSE:
// Checks the device of a staff session with Platform (Slice 1C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A positive answer may be kept 5 minutes by the caller; a refusal never (D-L5)
// - A new secret replaces the cookie at once (rotation)
// =========================================================
public sealed class VerifyOperationsDeviceUseCase
{
    private readonly IOperationsDeviceClient _devices;

    private readonly IOperationsSessionRepository _sessions;

    private readonly IOperationsRequestMetadata _request;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly OperationsDeviceBlocking _blocking;

    public VerifyOperationsDeviceUseCase(
        IOperationsDeviceClient devices,
        IOperationsSessionRepository sessions,
        IOperationsRequestMetadata request,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        OperationsDeviceBlocking blocking)
    {
        _blocking = blocking ?? throw new ArgumentNullException(nameof(blocking));
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<VerifyOperationsDeviceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        VerifyOperationsDeviceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (request.DeviceId == Guid.Empty || string.IsNullOrWhiteSpace(request.Secret))
        {
            return VerifyOperationsDeviceResponse.Success(false, "device_unknown", "none", null);
        }

        OperationsDeviceCheck check = await _devices.VerifyAsync(context.RequiredTenantId, request.DeviceId, request.Secret, true, _request.Country, cancellationToken);

        // Owner, R-6 (2026-10-21): a blocked device works for nobody until a company administrator unblocks it; its session ends.
        if (check.Allowed && await _blocking.IsBlockedAsync(request.DeviceId, cancellationToken))
        {
            await EndSessionAsync(context, cancellationToken);
            return VerifyOperationsDeviceResponse.Success(false, DeviceBlocked, check.Access, check.NewSecret);
        }

        // Owner, R-6: used from another country than the device's own (a trip during the session, or a session opened without the device), the device
        // is blocked, the session ends and the administrators are told. No country header at all (a request that did not come through Cloudflare)
        // tells nothing; Cloudflare's unknown (XX) and Tor (T1) do.
        if (check.Allowed && check.Country is { } registered && context.SessionId is { } sessionId && _request.Country is { } now && now != registered
            && await _sessions.GetAsync(sessionId, cancellationToken) is { IsEnded: false } session)
        {
            session.End(OperationsSessionEndReason.CountryChanged, _timeProvider.GetUtcNow());
            _audit.Record(session.UserId, "ops.auth.country_changed", nameof(OperationsUser), session.UserId,
                arguments: new { device = check.Number, registered, country = now, during = "session" });
            await _blocking.BlockAsync(context.RequiredTenantId, request.DeviceId, check.Name, check.Number, registered, now, session.UserId, request.PortalSlug,
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return VerifyOperationsDeviceResponse.Success(false, DeviceBlocked, check.Access, check.NewSecret);
        }

        return VerifyOperationsDeviceResponse.Success(check.Allowed, check.Code, check.Access, check.NewSecret);
    }

    public const string CountryChanged = "country_changed";

    public const string DeviceBlocked = "device_blocked";

    // =====================================================
    // METHOD: END SESSION
    // =====================================================
    private async Task EndSessionAsync(OperationsAuthorizationContext context, CancellationToken cancellationToken)
    {
        if (context.SessionId is { } sessionId && await _sessions.GetAsync(sessionId, cancellationToken) is { IsEnded: false } session)
        {
            session.End(OperationsSessionEndReason.CountryChanged, _timeProvider.GetUtcNow());
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
