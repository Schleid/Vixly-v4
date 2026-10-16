using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.UseCases.Registration.StartPlatformTenantRegistration;

// =========================================================
// FILE: StartPlatformTenantRegistrationUseCase.cs
//
// PURPOSE:
// Starts a company's self-service registration (D-T4, D-T18).
//
// RESPONSIBILITIES:
// - Check the bot token before any other work (D-T12)
// - Require the current terms and privacy versions
// - Claim the slug for 24 hours; create the registration with a
//   hashed access token; send the first verification link
//
// IMPORTANT NOTES:
// - Anonymous and rate-limited (S-6)
// - No tenant exists until the email is verified (D-T9)
// - The same email may register more than once: the answer never
//   says whether an address is known
// - The access token is returned once; only its hash is stored
// =========================================================
public sealed class StartPlatformTenantRegistrationUseCase
{
    private readonly IPlatformBotCheck _botCheck;

    private readonly IPlatformRequestMetadata _metadata;

    private readonly IPlatformTenantPolicy _policy;

    private readonly PlatformPortalSlugs _slugs;

    private readonly IPlatformPortalSlugRepository _slugClaims;

    private readonly IPlatformTenantRegistrationRepository _registrations;

    private readonly PlatformRegistrationLinks _links;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformTenantMailer _mailer;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly IPlatformLegalRepository _legal;

    public StartPlatformTenantRegistrationUseCase(
        IPlatformBotCheck botCheck,
        IPlatformRequestMetadata metadata,
        IPlatformTenantPolicy policy,
        PlatformPortalSlugs slugs,
        IPlatformPortalSlugRepository slugClaims,
        IPlatformTenantRegistrationRepository registrations,
        PlatformRegistrationLinks links,
        IPlatformSecretGenerator secrets,
        IPlatformTenantMailer mailer,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IPlatformLegalRepository legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
        _botCheck = botCheck ?? throw new ArgumentNullException(nameof(botCheck));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _slugs = slugs ?? throw new ArgumentNullException(nameof(slugs));
        _slugClaims = slugClaims ?? throw new ArgumentNullException(nameof(slugClaims));
        _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<StartPlatformTenantRegistrationResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        StartPlatformTenantRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (!await _botCheck.VerifyAsync(request.BotToken, _metadata.ClientIp, cancellationToken))
        {
            return StartPlatformTenantRegistrationResponse.Failure(PlatformRegistrationFlowErrors.BotCheckFailed);
        }

        if (!request.AcceptedTerms)
        {
            return StartPlatformTenantRegistrationResponse.Failure(PlatformRegistrationErrors.TermsNotAccepted);
        }

        // Slice 8: the published Terms (and the Privacy version it includes); before the first publish, the configured versions.
        Vixly.Platform.Domain.Legal.PlatformLegalDocument? terms = await _legal.GetPublishedAsync(Vixly.Platform.Domain.Legal.PlatformLegalDocumentKind.Terms, cancellationToken);
        string termsVersion = terms?.Version ?? _policy.TermsVersion;
        string privacyVersion = terms is null
            ? _policy.PrivacyVersion
            : terms.Parts.FirstOrDefault(part => part.Kind == Vixly.Platform.Domain.Legal.PlatformLegalDocumentKind.Privacy)?.Version ?? string.Empty;

        if (request.TermsVersion != termsVersion || request.PrivacyVersion != privacyVersion)
        {
            return StartPlatformTenantRegistrationResponse.Failure(PlatformRegistrationErrors.LegalVersionInvalid);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        PlatformSlugCheck slug = await _slugs.CheckAsync(request.Slug, utcNow, cancellationToken);

        if (slug.Error is not null)
        {
            return StartPlatformTenantRegistrationResponse.Failure(slug.Error);
        }

        PlatformSecret access = _secrets.NewToken();
        PlatformTenantRegistration registration;
        PlatformSecret link;

        try
        {
            registration = PlatformTenantRegistration.Start(
                request.Company.ToDetails(),
                request.OwnerName,
                EmailAddress.Create(request.OwnerEmail),
                PhoneNumber.Create(request.OwnerPhone),
                LanguageCode.Create(request.Language),
                request.TimeZone,
                slug.Slug!,
                access.Hash,
                request.AcceptedTerms,
                request.TermsVersion,
                request.PrivacyVersion,
                _metadata.ClientIp,
                utcNow);

            _registrations.Add(registration);

            if (slug.LapsedClaim is { } lapsed)
            {
                lapsed.ReclaimForRegistration(registration.Id, registration.VerificationDeadlineUtc, utcNow);
            }
            else
            {
                _slugClaims.Add(PlatformPortalSlug.ClaimForRegistration(
                    slug.Slug!,
                    registration.Id,
                    registration.VerificationDeadlineUtc,
                    utcNow));
            }

            link = await _links.IssueAsync(registration, utcNow, cancellationToken);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return StartPlatformTenantRegistrationResponse.Failure(exception.Error);
        }

        _audit.Record(
            null,
            "platform.registrations.started",
            null,
            nameof(PlatformTenantRegistration),
            registration.Id,
            after: new { slug = registration.Slug, language = registration.Language.Value, terms = registration.TermsVersion, privacy = registration.PrivacyVersion });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return StartPlatformTenantRegistrationResponse.Failure(PlatformTenantErrors.SlugTaken);
        }

        await _mailer.SendRegistrationLinkAsync(registration, link.Value, cancellationToken);

        return StartPlatformTenantRegistrationResponse.Success(access.Value, registration.AccessExpiresUtc);
    }
}
