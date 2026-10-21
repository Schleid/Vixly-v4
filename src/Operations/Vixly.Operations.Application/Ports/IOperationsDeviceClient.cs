namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsDeviceClient.cs
//
// PURPOSE:
// Platform's device calls (Slice 7, exposed by Slice 9) for Operations (Slice 1C).
//
// RESPONSIBILITIES:
// - Ask for the first-device link
// - Complete an emailed device link
// - Enrol, list, approve, deny and release devices
// - Check a device on every session
//
// IMPORTANT NOTES:
// - Platform owns devices, seats and secrets; Operations keeps only the device cookie
// - Refusals come back as ops.devices.* errors
// =========================================================
public interface IOperationsDeviceClient
{
    Task<OperationsPlatformCall<bool>> RequestFirstDeviceLinkAsync(OperationsPlatformActor actor, CancellationToken cancellationToken);

    Task<OperationsPlatformCall<OperationsDeviceEnrolment>> CompleteLinkAsync(Guid tenantId, string token, string name, string kind, CancellationToken cancellationToken);

    Task<OperationsPlatformCall<OperationsDeviceEnrolment>> EnrolAsync(OperationsPlatformActor actor, string name, string kind, CancellationToken cancellationToken);

    // Support's second-factor reset link (D-O13): Platform checks it belongs to this tenant and spends it.
    Task<OperationsPlatformCall<bool>> CompleteSecondFactorResetAsync(Guid tenantId, string token, CancellationToken cancellationToken);

    Task<OperationsDeviceCheck> VerifyAsync(Guid tenantId, Guid deviceId, string secret, CancellationToken cancellationToken);

    Task<OperationsPlatformCall<IReadOnlyList<OperationsDevice>>> ListAsync(OperationsPlatformActor actor, CancellationToken cancellationToken);

    Task<OperationsPlatformCall<bool>> ApproveAsync(OperationsPlatformActor actor, Guid deviceId, CancellationToken cancellationToken);

    Task<OperationsPlatformCall<bool>> DenyAsync(OperationsPlatformActor actor, Guid deviceId, CancellationToken cancellationToken);

    Task<OperationsPlatformCall<bool>> ReleaseAsync(OperationsPlatformActor actor, Guid deviceId, string reason, CancellationToken cancellationToken);
}
