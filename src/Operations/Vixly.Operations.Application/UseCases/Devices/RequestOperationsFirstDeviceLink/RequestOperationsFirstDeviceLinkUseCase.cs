using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.RequestOperationsFirstDeviceLink;

// =========================================================
// FILE: RequestOperationsFirstDeviceLinkUseCase.cs
//
// PURPOSE:
// Asks Platform to email the Owner the link that enrols their first computer and activates the tenant (Slice 1C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Owner only, during setup; Platform sends the email (onboarding step 5)
// - Platform refuses when the registration is not ready or the tenant already has a device
// =========================================================
public sealed class RequestOperationsFirstDeviceLinkUseCase
{
    private readonly IOperationsUserRepository _users;

    private readonly OperationsActors _actors;

    private readonly IOperationsDeviceClient _devices;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public RequestOperationsFirstDeviceLinkUseCase(
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

    public async Task<RequestOperationsFirstDeviceLinkResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RequestOperationsFirstDeviceLinkRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsUser? user = context.UserId is { } userId ? await _users.GetAsync(userId, cancellationToken) : null;

        if (user is null)
        {
            return RequestOperationsFirstDeviceLinkResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!user.IsOwner)
        {
            return RequestOperationsFirstDeviceLinkResponse.Failure(OperationsAccessErrors.OwnerOnly);
        }

        OperationsPlatformCall<bool> call = await _devices.RequestFirstDeviceLinkAsync(_actors.For(context, user), cancellationToken);

        if (!call.Succeeded)
        {
            return RequestOperationsFirstDeviceLinkResponse.Failure(call.Error!);
        }

        _audit.Record(user.Id, "ops.devices.first_link_requested", nameof(OperationsUser), user.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return RequestOperationsFirstDeviceLinkResponse.Success();
    }
}
