using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Registration.VerifyRegistrationEmail;

// =========================================================
// FILE: VerifyRegistrationEmailUseCase.cs
//
// PURPOSE:
// Verifies the registrant's email from the link and creates the
// Draft tenant (D-T9).
//
// RESPONSIBILITIES:
// - Consume the single-use link
// - Create the tenant with its owner contact and consent history
// - Make the slug claim permanent; move the registration to
//   "choose a plan"; replace the access token
//
// IMPORTANT NOTES:
// - Anonymous and rate-limited (S-6)
// - Expired, used and unknown links give one error
// - The person holding the email link gets the new access token:
//   email ownership is the stronger proof
// =========================================================
public sealed class VerifyRegistrationEmailUseCase
{
    private readonly IPlatformTenantRegistrationRepository _registrations;

    private readonly IPlatformPortalSlugRepository _slugClaims;

    private readonly IPlatformTenantRepository _tenants;

    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public VerifyRegistrationEmailUseCase(
        IPlatformTenantRegistrationRepository registrations,
        IPlatformPortalSlugRepository slugClaims,
        IPlatformTenantRepository tenants,
        PlatformTenantAdministration administration,
        IPlatformSecretGenerator secrets,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        _slugClaims = slugClaims ?? throw new ArgumentNullException(nameof(slugClaims));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<VerifyRegistrationEmailResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        VerifyRegistrationEmailRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return VerifyRegistrationEmailResponse.Failure(PlatformRegistrationErrors.LinkInvalid);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        PlatformRegistrationToken? link = await _registrations.FindTokenByHashAsync(_secrets.HashToken(request.Token.Trim()), cancellationToken);
        PlatformTenantRegistration? registration = link is null
            ? null
            : await _registrations.GetAsync(link.PlatformTenantRegistrationId, cancellationToken);

        if (link is null || registration is null)
        {
            return VerifyRegistrationEmailResponse.Failure(PlatformRegistrationErrors.LinkInvalid);
        }

        try
        {
            link.Consume(utcNow);
        }
        catch (DomainException exception)
        {
            return VerifyRegistrationEmailResponse.Failure(exception.Error);
        }

        if (registration.ExpireIfDue(utcNow))
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return VerifyRegistrationEmailResponse.Failure(PlatformRegistrationErrors.Expired);
        }

        PlatformPortalSlug? claim = await _slugClaims.FindAsync(registration.Slug, cancellationToken);

        if (claim is null || !claim.IsHeldBy(registration.Id))
        {
            _unitOfWork.DiscardChanges();
            return VerifyRegistrationEmailResponse.Failure(PlatformTenantErrors.SlugTaken);
        }

        string? code = await _administration.NewTenantCodeAsync(cancellationToken);

        if (code is null)
        {
            _unitOfWork.DiscardChanges();
            return VerifyRegistrationEmailResponse.Failure(PlatformTenantAdminErrors.CodeUnavailable);
        }

        PlatformSecret access = _secrets.NewToken();
        PlatformTenantCreation creation;

        try
        {
            creation = PlatformTenant.CreateFromRegistration(code, registration, utcNow);
            registration.MarkEmailVerified(creation.Tenant.Id, utcNow);
            registration.ReplaceAccessToken(access.Hash, utcNow);
            claim.MakePermanent(registration.Id, creation.Tenant.Id, utcNow);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return VerifyRegistrationEmailResponse.Failure(exception.Error);
        }

        _tenants.Add(creation.Tenant);
        _tenants.AddEvents(creation.Events);
        _audit.Record(null, "platform.registrations.email_verified", null, nameof(PlatformTenantRegistration), registration.Id);
        _audit.Record(
            null,
            "platform.tenants.created",
            null,
            nameof(PlatformTenant),
            creation.Tenant.Id,
            after: new { code, slug = creation.Tenant.CurrentSlug, origin = "self_registration", registration = registration.Id });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return VerifyRegistrationEmailResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return VerifyRegistrationEmailResponse.Success(access.Value, code);
    }
}
