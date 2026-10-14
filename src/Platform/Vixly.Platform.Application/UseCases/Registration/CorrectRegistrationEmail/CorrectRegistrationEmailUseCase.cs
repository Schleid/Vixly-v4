using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.UseCases.Registration.CorrectRegistrationEmail;

// =========================================================
// FILE: CorrectRegistrationEmailUseCase.cs
//
// PURPOSE:
// Corrects a mistyped owner email before verification and sends
// a new link.
//
// RESPONSIBILITIES:
// - Open the registration from its access token
// - Change the email, revoke older links, send a new one
//
// IMPORTANT NOTES:
// - Anonymous and rate-limited (S-6)
// - Only before verification (V3 had it unused)
// =========================================================
public sealed class CorrectRegistrationEmailUseCase
{
    private readonly PlatformRegistrationLinks _links;

    private readonly IPlatformTenantMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CorrectRegistrationEmailUseCase(
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

    public async Task<CorrectRegistrationEmailResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        CorrectRegistrationEmailRequest request,
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

            return CorrectRegistrationEmailResponse.Failure(access.Error!);
        }

        PlatformSecret link;

        try
        {
            registration.CorrectOwnerEmail(EmailAddress.Create(request.Email), utcNow);
            link = await _links.IssueAsync(registration, utcNow, cancellationToken);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return CorrectRegistrationEmailResponse.Failure(exception.Error);
        }

        _audit.Record(null, "platform.registrations.email_corrected", null, nameof(PlatformTenantRegistration), registration.Id);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CorrectRegistrationEmailResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        await _mailer.SendRegistrationLinkAsync(registration, link.Value, cancellationToken);

        return CorrectRegistrationEmailResponse.Success();
    }
}
