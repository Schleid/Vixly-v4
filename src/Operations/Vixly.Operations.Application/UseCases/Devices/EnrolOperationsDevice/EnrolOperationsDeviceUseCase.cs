using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.EnrolOperationsDevice;

// =========================================================
// FILE: EnrolOperationsDeviceUseCase.cs
//
// PURPOSE:
// Asks to add the computer a signed-in staff member is using; an Owner approves it (Slice 1C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The device waits ("pending") until an Owner approves it with a fresh code
// =========================================================
public sealed class EnrolOperationsDeviceUseCase
{
    private readonly IOperationsUserRepository _users;

    private readonly OperationsActors _actors;

    private readonly IOperationsDeviceClient _devices;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public EnrolOperationsDeviceUseCase(
        IOperationsUserRepository users,
        OperationsActors actors,
        IOperationsDeviceClient devices,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<EnrolOperationsDeviceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        EnrolOperationsDeviceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsUser? user = context.UserId is { } userId ? await _users.GetAsync(userId, cancellationToken) : null;

        if (user is null)
        {
            return EnrolOperationsDeviceResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        string name = (request.Name ?? string.Empty).Trim();

        if (name.Length == 0 || name.Length > OperationsDeviceErrors.NameMaxLength)
        {
            return EnrolOperationsDeviceResponse.Failure(OperationsDeviceErrors.NameRequired);
        }

        if (!OperationsDeviceErrors.Kinds.Contains(request.Kind, StringComparer.Ordinal))
        {
            return EnrolOperationsDeviceResponse.Failure(OperationsDeviceErrors.KindInvalid);
        }

        OperationsPlatformCall<OperationsDeviceEnrolment> call = await _devices.EnrolAsync(_actors.For(context, user), name, request.Kind, cancellationToken);

        if (!call.Succeeded)
        {
            return EnrolOperationsDeviceResponse.Failure(call.Error!);
        }

        _audit.Record(user.Id, "ops.devices.requested", "Device", call.Value!.DeviceId, arguments: new { number = call.Value.Number });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return EnrolOperationsDeviceResponse.Success(call.Value.DeviceId, call.Value.Secret ?? string.Empty, call.Value.Status);
    }
}
