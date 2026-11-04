using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeDevices.cs
//
// PURPOSE:
// Platform's devices as the sign-in sees them: verify a device's
// cookie and tell its name, number and country (owner,
// 2026-10-21).
//
// RESPONSIBILITIES:
// - Hold the devices a test registers; answer VerifyAsync
//
// IMPORTANT NOTES:
// - Tests only; what the sign-in tests never call is not supported
// =========================================================
internal sealed class FakeDevices : IOperationsDeviceClient
{
    public Dictionary<Guid, (string Secret, string Name, string Number, string? Country)> Devices { get; } = new();

    public Guid Register(string secret, string? country)
    {
        Guid id = Guid.NewGuid();
        Devices[id] = (secret, "Front desk", "D-000001", country);
        return id;
    }

    public Task<OperationsDeviceCheck> VerifyAsync(Guid tenantId, Guid deviceId, string secret, bool rotate, string? country, CancellationToken cancellationToken) =>
        Task.FromResult(Devices.TryGetValue(deviceId, out var device) && device.Secret == secret
            ? new OperationsDeviceCheck(true, null, "full", null, device.Name, device.Number, device.Country)
            : new OperationsDeviceCheck(false, "device_unknown", "none", null));

    public Task<OperationsPlatformCall<bool>> RequestFirstDeviceLinkAsync(OperationsPlatformActor actor, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<OperationsPlatformCall<OperationsDeviceEnrolment>> CompleteLinkAsync(Guid tenantId, string token, string name, string kind, OperationsDeviceOrigin origin,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<OperationsPlatformCall<OperationsDeviceEnrolment>> EnrolAsync(OperationsPlatformActor actor, string name, string kind, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<OperationsPlatformCall<bool>> CompleteSecondFactorResetAsync(Guid tenantId, string token, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<OperationsPlatformCall<IReadOnlyList<OperationsDevice>>> ListAsync(OperationsPlatformActor actor, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<OperationsPlatformCall<OperationsDeviceSeats>> SeatsAsync(OperationsPlatformActor actor, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<OperationsPlatformCall<bool>> ApproveAsync(OperationsPlatformActor actor, Guid deviceId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<OperationsPlatformCall<bool>> DenyAsync(OperationsPlatformActor actor, Guid deviceId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<OperationsPlatformCall<bool>> ReleaseAsync(OperationsPlatformActor actor, Guid deviceId, string reason, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
