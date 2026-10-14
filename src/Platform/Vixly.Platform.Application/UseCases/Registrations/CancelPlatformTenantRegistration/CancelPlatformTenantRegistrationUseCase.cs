using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Registrations.CancelPlatformTenantRegistration;

// =========================================================
// FILE: CancelPlatformTenantRegistrationUseCase.cs
//
// PURPOSE:
// Cancels an incomplete registration with a reason.
//
// RESPONSIBILITIES:
// - Check platform.tenants.edit
// - Cancel it and free its pending slug
//
// IMPORTANT NOTES:
// - Nothing is deleted: the company can register again (D-T11)
// - A Draft tenant created at verification stays for staff to
//   deactivate
// =========================================================
public sealed class CancelPlatformTenantRegistrationUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformTenantRegistrationRepository _registrations;

    private readonly PlatformRegistrationLinks _links;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CancelPlatformTenantRegistrationUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformTenantRegistrationRepository registrations,
        PlatformRegistrationLinks links,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CancelPlatformTenantRegistrationResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CancelPlatformTenantRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.TenantsEdit, cancellationToken);

        PlatformTenantRegistration? registration = await _registrations.GetAsync(request.RegistrationId, cancellationToken);

        if (registration is null)
        {
            return CancelPlatformTenantRegistrationResponse.Failure(PlatformCommonErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            registration.Cancel(context.PlatformUserId, request.Reason, utcNow);
            await _links.ReleasePendingSlugAsync(registration, cancellationToken);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return CancelPlatformTenantRegistrationResponse.Failure(exception.Error);
        }

        _audit.Record(context.PlatformUserId, "platform.registrations.cancelled", PlatformPermissions.TenantsEdit, nameof(PlatformTenantRegistration), registration.Id, reason: registration.CancellationReason);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CancelPlatformTenantRegistrationResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return CancelPlatformTenantRegistrationResponse.Success();
    }
}
