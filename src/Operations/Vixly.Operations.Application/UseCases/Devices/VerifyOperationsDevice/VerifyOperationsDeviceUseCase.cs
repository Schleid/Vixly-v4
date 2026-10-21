using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
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

    public VerifyOperationsDeviceUseCase(
        IOperationsDeviceClient devices)
    {
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
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

        OperationsDeviceCheck check = await _devices.VerifyAsync(context.RequiredTenantId, request.DeviceId, request.Secret, cancellationToken);
        return VerifyOperationsDeviceResponse.Success(check.Allowed, check.Code, check.Access, check.NewSecret);
    }
}
