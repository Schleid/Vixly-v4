using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.UseCases.Devices.UnblockOperationsDevice;

// =========================================================
// FILE: UnblockOperationsDeviceUseCase.cs
//
// PURPOSE:
// Lifts the block of a device used from another country, with a fresh authenticator code (owner, R-6).
//
// RESPONSIBILITIES:
// - Allow only a company administrator (the right to approve devices), confirmed by a fresh code
// - Lift the open block and record who did it
//
// IMPORTANT NOTES:
// - Owner's words: "block sign-in until company system admin unblock them"
// - The device's next sign-in starts from scratch: password, then the second step as usual
// =========================================================
public sealed class UnblockOperationsDeviceUseCase
{
    private readonly IOperationsUserRepository _users;

    private readonly OperationsAccess _access;

    private readonly OperationsStepUp _stepUp;

    private readonly IOperationsDeviceBlockRepository _blocks;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public UnblockOperationsDeviceUseCase(
        IOperationsUserRepository users,
        OperationsAccess access,
        OperationsStepUp stepUp,
        IOperationsDeviceBlockRepository blocks,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<UnblockOperationsDeviceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        UnblockOperationsDeviceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // =====================================================
        // TRUSTED CONTEXT VALIDATION
        // =====================================================
        OperationsUser? user = context.UserId is { } userId ? await _users.GetAsync(userId, cancellationToken) : null;

        if (user is null)
        {
            return UnblockOperationsDeviceResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _access.LoadAsync(context, cancellationToken) is not { } access || !access.HasTenantWide(Vixly.Operations.Domain.Access.OperationsPermissions.DevicesApprove))
        {
            return UnblockOperationsDeviceResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // =====================================================
        // REQUEST VALIDATION
        // =====================================================
        if (await _blocks.FindOpenAsync(request.DeviceId, cancellationToken) is not { } block)
        {
            return UnblockOperationsDeviceResponse.Failure(OperationsCommonErrors.NotFound);
        }

        if (await _stepUp.CheckAsync(user, request.Code, cancellationToken) is { } stepUpRefused)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return UnblockOperationsDeviceResponse.Failure(stepUpRefused);
        }

        // =====================================================
        // UNBLOCK
        // =====================================================
        block.Unblock(user.Id, _timeProvider.GetUtcNow());
        _audit.Record(user.Id, "ops.devices.unblocked", "Device", block.DeviceId, arguments: new { device = block.DeviceNumber, country = block.Country });

        // =====================================================
        // PERSISTENCE
        // =====================================================
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UnblockOperationsDeviceResponse.Success()
            : UnblockOperationsDeviceResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
