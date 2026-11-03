using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.UseCases.SignIn.ListOperationsDevicePeople;

// =========================================================
// FILE: ListOperationsDevicePeopleUseCase.cs
//
// PURPOSE:
// The people the PIN screen of a licensed device offers (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only on a licensed device of the company; elsewhere nobody is listed
// - People without a PIN, or whose account cannot sign in, are left out
// =========================================================
public sealed class ListOperationsDevicePeopleUseCase
{
    private readonly IOperationsQuickSignInRepository _quick;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsDeviceClient _devices;

    private readonly TimeProvider _timeProvider;

    public ListOperationsDevicePeopleUseCase(
        IOperationsQuickSignInRepository quick,
        IOperationsUserRepository users,
        IOperationsDeviceClient devices,
        TimeProvider timeProvider)
    {
        _quick = quick ?? throw new ArgumentNullException(nameof(quick));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListOperationsDevicePeopleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsDevicePeopleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (request.DeviceId is not { } deviceId || deviceId == Guid.Empty || string.IsNullOrWhiteSpace(request.DeviceSecret))
        {
            return ListOperationsDevicePeopleResponse.Failure(OperationsAuthErrors.QuickDeviceRequired);
        }

        try
        {
            if (!(await _devices.VerifyAsync(context.RequiredTenantId, deviceId, request.DeviceSecret, false, null, cancellationToken)).Allowed)
            {
                return ListOperationsDevicePeopleResponse.Failure(OperationsAuthErrors.QuickDeviceRequired);
            }
        }
        catch (Vixly.Operations.Application.Platform.OperationsPlatformUnavailableException)
        {
            return ListOperationsDevicePeopleResponse.Failure(OperationsAuthErrors.QuickDeviceRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        List<OperationsQuickPerson> people = new();

        foreach (OperationsDevicePerson person in await _quick.ListDevicePeopleAsync(deviceId, utcNow - OperationsQuickSignIn.DevicePeopleWindow, cancellationToken))
        {
            if (await _users.GetAsync(person.UserId, cancellationToken) is { } user && user.Account.IsSignInAllowed(utcNow)
                && await _quick.FindPinAsync(user.Id, cancellationToken) is { } pin)
            {
                people.Add(new OperationsQuickPerson(user.Id, user.FullName, pin.IsLocked));
            }
        }

        return ListOperationsDevicePeopleResponse.Success(people);
    }
}
