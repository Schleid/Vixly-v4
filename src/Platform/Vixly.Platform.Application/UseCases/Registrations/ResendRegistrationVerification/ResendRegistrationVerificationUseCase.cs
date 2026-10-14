using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Registrations.ResendRegistrationVerification;

// =========================================================
// FILE: ResendRegistrationVerificationUseCase.cs
//
// PURPOSE:
// Sends a registrant a new verification link on staff request.
//
// RESPONSIBILITIES:
// - Check platform.tenants.edit
// - Revoke older links and send a new one
//
// IMPORTANT NOTES:
// - Same throttle as the public resend
// =========================================================
public sealed class ResendRegistrationVerificationUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformTenantRegistrationRepository _registrations;

    private readonly PlatformRegistrationLinks _links;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly IPlatformTenantMailer _mailer;

    public ResendRegistrationVerificationUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformTenantRegistrationRepository registrations,
        PlatformRegistrationLinks links,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IPlatformTenantMailer mailer)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
    }

    public async Task<ResendRegistrationVerificationResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ResendRegistrationVerificationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.TenantsEdit, cancellationToken);

        PlatformTenantRegistration? registration = await _registrations.GetAsync(request.RegistrationId, cancellationToken);

        if (registration is null)
        {
            return ResendRegistrationVerificationResponse.Failure(PlatformCommonErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        PlatformSecret? link = null;

        try
        {
            _ = registration.ExpireIfDue(utcNow);
            link = await _links.IssueAsync(registration, utcNow, cancellationToken);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return ResendRegistrationVerificationResponse.Failure(exception.Error);
        }

        _audit.Record(context.PlatformUserId, "platform.registrations.verification_resent", PlatformPermissions.TenantsEdit, nameof(PlatformTenantRegistration), registration.Id);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ResendRegistrationVerificationResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendRegistrationLinkAsync(registration, link!.Value, cancellationToken);

        return ResendRegistrationVerificationResponse.Success();
    }
}
