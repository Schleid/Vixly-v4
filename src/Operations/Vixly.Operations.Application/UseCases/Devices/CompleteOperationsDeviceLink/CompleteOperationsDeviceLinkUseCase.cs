using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.CompleteOperationsDeviceLink;

// =========================================================
// FILE: CompleteOperationsDeviceLinkUseCase.cs
//
// PURPOSE:
// Completes an emailed device link (first device or owner recovery) on the computer that opened it (Slice 1C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The link proves the Owner; no sign-in is needed (Platform Slice 7, D-L7, D-L8)
// - The tenant of the host is sent, so a link of another tenant is refused
// - The first-device link activates the tenant in Platform's commit
// =========================================================
public sealed class CompleteOperationsDeviceLinkUseCase
{
    private readonly IOperationsDeviceClient _devices;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public CompleteOperationsDeviceLinkUseCase(
        IOperationsDeviceClient devices,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CompleteOperationsDeviceLinkResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CompleteOperationsDeviceLinkRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        string name = (request.Name ?? string.Empty).Trim();

        if (name.Length == 0 || name.Length > OperationsDeviceErrors.NameMaxLength)
        {
            return CompleteOperationsDeviceLinkResponse.Failure(OperationsDeviceErrors.NameRequired);
        }

        if (!OperationsDeviceErrors.Kinds.Contains(request.Kind, StringComparer.Ordinal))
        {
            return CompleteOperationsDeviceLinkResponse.Failure(OperationsDeviceErrors.KindInvalid);
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return CompleteOperationsDeviceLinkResponse.Failure(OperationsDeviceErrors.LinkInvalid);
        }

        OperationsPlatformCall<OperationsDeviceEnrolment> call = await _devices.CompleteLinkAsync(context.RequiredTenantId, request.Token.Trim(), name, request.Kind, cancellationToken);

        if (!call.Succeeded)
        {
            return CompleteOperationsDeviceLinkResponse.Failure(call.Error!);
        }

        if (call.Value!.Secret is not { Length: > 0 } secret)
        {
            return CompleteOperationsDeviceLinkResponse.Failure(OperationsDeviceErrors.Refused);
        }

        _audit.Record(null, "ops.devices.enrolled_by_link", "Device", call.Value.DeviceId, arguments: new { number = call.Value.Number });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CompleteOperationsDeviceLinkResponse.Success(call.Value.DeviceId, secret);
    }
}
