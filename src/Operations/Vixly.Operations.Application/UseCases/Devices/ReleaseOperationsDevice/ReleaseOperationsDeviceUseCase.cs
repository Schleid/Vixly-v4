using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.ReleaseOperationsDevice;

// =========================================================
// FILE: ReleaseOperationsDeviceUseCase.cs
//
// PURPOSE:
// Stops a device (lost, broken, staff left, ...) with a fresh authenticator code (Slice 1C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Releases within the allowance free the seat at once; past it Vixly staff review (Platform Slice 7)
// =========================================================
public sealed class ReleaseOperationsDeviceUseCase
{
    // Platform's release reasons a tenant may give (Slice 7).
    public static IReadOnlyList<string> Reasons { get; } = new[] { "lost_or_stolen", "broken", "staff_left", "replaced", "other" };

    private readonly IOperationsUserRepository _users;

    private readonly OperationsAccess _access;

    private readonly OperationsActors _actors;

    private readonly OperationsStepUp _stepUp;

    private readonly IOperationsDeviceClient _devices;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public ReleaseOperationsDeviceUseCase(
        IOperationsUserRepository users,
        OperationsAccess access,
        OperationsActors actors,
        OperationsStepUp stepUp,
        IOperationsDeviceClient devices,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ReleaseOperationsDeviceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ReleaseOperationsDeviceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsUser? user = context.UserId is { } userId ? await _users.GetAsync(userId, cancellationToken) : null;

        if (user is null)
        {
            return ReleaseOperationsDeviceResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _access.LoadAsync(context, cancellationToken) is not { } access || !access.HasTenantWide(Vixly.Operations.Domain.Access.OperationsPermissions.DevicesRelease))
        {
            return ReleaseOperationsDeviceResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (!Reasons.Contains(request.Reason, StringComparer.Ordinal))
        {
            return ReleaseOperationsDeviceResponse.Failure(OperationsCommonErrors.RequestInvalid);
        }

        if (_stepUp.Check(user, request.Code) is { } stepUpRefused)
        {
            return ReleaseOperationsDeviceResponse.Failure(stepUpRefused);
        }

        OperationsPlatformCall<bool> call = await _devices.ReleaseAsync(_actors.For(context, user, stepUp: true), request.DeviceId, request.Reason, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!call.Succeeded)
        {
            return ReleaseOperationsDeviceResponse.Failure(call.Error!);
        }

        _audit.Record(user.Id, "ops.devices.released", "Device", request.DeviceId, arguments: new { reason = request.Reason });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ReleaseOperationsDeviceResponse.Success();
    }
}
