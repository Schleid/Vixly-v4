using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.ListOperationsDevices;

// =========================================================
// FILE: ListOperationsDevicesUseCase.cs
//
// PURPOSE:
// Lists the tenant's devices for the Owner (Slice 1C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs the permission; device approval and release belong to the Owner and Administrators only (design section 3)
// =========================================================
public sealed class ListOperationsDevicesUseCase
{
    private readonly IOperationsUserRepository _users;

    private readonly OperationsAccess _access;

    private readonly OperationsActors _actors;

    private readonly IOperationsDeviceClient _devices;

    public ListOperationsDevicesUseCase(
        IOperationsUserRepository users,
        OperationsAccess access,
        OperationsActors actors,
        IOperationsDeviceClient devices)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
    }

    public async Task<ListOperationsDevicesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsDevicesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsUser? user = context.UserId is { } userId ? await _users.GetAsync(userId, cancellationToken) : null;

        if (user is null)
        {
            return ListOperationsDevicesResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _access.LoadAsync(context, cancellationToken) is not { } access || !access.HasTenantWide(Vixly.Operations.Domain.Access.OperationsPermissions.DevicesView))
        {
            return ListOperationsDevicesResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        OperationsPlatformActor actor = _actors.For(context, user);
        OperationsPlatformCall<IReadOnlyList<OperationsDevice>> call = await _devices.ListAsync(actor, cancellationToken);

        if (!call.Succeeded)
        {
            return ListOperationsDevicesResponse.Failure(call.Error!);
        }

        OperationsPlatformCall<OperationsDeviceSeats> seats = await _devices.SeatsAsync(actor, cancellationToken);
        return ListOperationsDevicesResponse.Success(call.Value!, seats.Succeeded ? seats.Value : null);
    }
}
