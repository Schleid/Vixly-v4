using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Registration.ResendMyRegistrationVerification;

// =========================================================
// FILE: ResendMyRegistrationVerificationUseCase.cs
//
// PURPOSE:
// Sends the registrant a new verification link.
//
// RESPONSIBILITIES:
// - Open the registration from its access token
// - Revoke older links and send a new one
//
// IMPORTANT NOTES:
// - Anonymous and rate-limited; 3 links per hour per registration
// =========================================================
public sealed class ResendMyRegistrationVerificationUseCase
{
    private readonly PlatformRegistrationLinks _links;

    private readonly IPlatformTenantMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ResendMyRegistrationVerificationUseCase(
        PlatformRegistrationLinks links,
        IPlatformTenantMailer mailer,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ResendMyRegistrationVerificationResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        ResendMyRegistrationVerificationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        PlatformRegistrationAccess access = await _links.OpenAsync(request.AccessToken, utcNow, cancellationToken);

        if (access.Registration is not PlatformTenantRegistration registration)
        {
            if (access.ExpiredNow)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return ResendMyRegistrationVerificationResponse.Failure(access.Error!);
        }

        PlatformSecret link;

        try
        {
            link = await _links.IssueAsync(registration, utcNow, cancellationToken);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return ResendMyRegistrationVerificationResponse.Failure(exception.Error);
        }

        _audit.Record(null, "platform.registrations.verification_resent", null, nameof(PlatformTenantRegistration), registration.Id);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ResendMyRegistrationVerificationResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendRegistrationLinkAsync(registration, link.Value, cancellationToken);

        return ResendMyRegistrationVerificationResponse.Success();
    }
}
