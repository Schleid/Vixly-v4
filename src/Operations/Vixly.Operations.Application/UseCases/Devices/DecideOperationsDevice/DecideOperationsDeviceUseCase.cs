using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.DecideOperationsDevice;

// =========================================================
// FILE: DecideOperationsDeviceUseCase.cs
//
// PURPOSE:
// Approves or denies a computer waiting to join, with a fresh authenticator code (Slice 1C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs the permission; device approval and release belong to the Owner and Administrators only (design section 3); Platform checks the seat
// =========================================================
public sealed class DecideOperationsDeviceUseCase
{
    private readonly IOperationsUserRepository _users;

    private readonly OperationsAccess _access;

    private readonly OperationsActors _actors;

    private readonly OperationsStepUp _stepUp;

    private readonly IOperationsDeviceClient _devices;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public DecideOperationsDeviceUseCase(
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

    public async Task<DecideOperationsDeviceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        DecideOperationsDeviceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsUser? user = context.UserId is { } userId ? await _users.GetAsync(userId, cancellationToken) : null;

        if (user is null)
        {
            return DecideOperationsDeviceResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _access.LoadAsync(context, cancellationToken) is not { } access || !access.HasTenantWide(Vixly.Operations.Domain.Access.OperationsPermissions.DevicesApprove))
        {
            return DecideOperationsDeviceResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (_stepUp.Check(user, request.Code) is { } stepUpRefused)
        {
            return DecideOperationsDeviceResponse.Failure(stepUpRefused);
        }

        OperationsPlatformActor actor = _actors.For(context, user, stepUp: true);
        OperationsPlatformCall<bool> call = request.Approve
            ? await _devices.ApproveAsync(actor, request.DeviceId, cancellationToken)
            : await _devices.DenyAsync(actor, request.DeviceId, cancellationToken);

        // The code is spent whatever Platform answers.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!call.Succeeded)
        {
            return DecideOperationsDeviceResponse.Failure(call.Error!);
        }

        _audit.Record(user.Id, request.Approve ? "ops.devices.approved" : "ops.devices.denied", "Device", request.DeviceId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DecideOperationsDeviceResponse.Success();
    }
}
