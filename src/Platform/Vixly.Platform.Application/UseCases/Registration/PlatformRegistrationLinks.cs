using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.Registration;

// =========================================================
// FILE: PlatformRegistrationLinks.cs
//
// PURPOSE:
// Shared steps of the registration use cases.
//
// RESPONSIBILITIES:
// - Open a registration from its access token
// - Issue a new verification link: throttle, revoke older links,
//   extend the slug hold to the new deadline
//
// IMPORTANT NOTES:
// - Only token hashes are stored or looked up
// - Saving and sending stay with the use case: mail goes out
//   after the commit
// =========================================================
public sealed class PlatformRegistrationLinks
{
    private readonly IPlatformTenantRegistrationRepository _registrations;

    private readonly IPlatformPortalSlugRepository _slugs;

    private readonly IPlatformSecretGenerator _secrets;

    public PlatformRegistrationLinks(
        IPlatformTenantRegistrationRepository registrations,
        IPlatformPortalSlugRepository slugs,
        IPlatformSecretGenerator secrets)
    {
        _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        _slugs = slugs ?? throw new ArgumentNullException(nameof(slugs));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
    }

    public async Task<PlatformRegistrationAccess> OpenAsync(
        string? accessToken,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return PlatformRegistrationAccess.Refused(PlatformRegistrationErrors.AccessHashInvalid);
        }

        PlatformTenantRegistration? registration = await _registrations.FindByAccessHashAsync(
            _secrets.HashToken(accessToken.Trim()),
            cancellationToken);

        if (registration is null)
        {
            return PlatformRegistrationAccess.Refused(PlatformRegistrationErrors.AccessHashInvalid);
        }

        if (registration.ExpireIfDue(utcNow))
        {
            return PlatformRegistrationAccess.Refused(PlatformRegistrationErrors.Expired, expiredNow: true);
        }

        if (!registration.AcceptsAccess(utcNow))
        {
            return PlatformRegistrationAccess.Refused(registration.Status switch
            {
                PlatformTenantRegistrationStatus.Cancelled => PlatformRegistrationErrors.Cancelled,
                PlatformTenantRegistrationStatus.Expired => PlatformRegistrationErrors.Expired,
                _ => PlatformRegistrationErrors.AccessHashInvalid
            });
        }

        return PlatformRegistrationAccess.Granted(registration);
    }

    public async Task<PlatformSecret> IssueAsync(
        PlatformTenantRegistration registration,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);

        registration.RecordVerificationSent(utcNow);

        foreach (PlatformRegistrationToken older in await _registrations.ListUnusedTokensAsync(registration.Id, cancellationToken))
        {
            older.Revoke(utcNow);
        }

        PlatformSecret token = _secrets.NewToken();
        _registrations.AddToken(PlatformRegistrationToken.Issue(registration.Id, token.Hash, utcNow));

        if (await _slugs.FindAsync(registration.Slug, cancellationToken) is { } claim && claim.IsHeldBy(registration.Id))
        {
            claim.ExtendHold(registration.Id, registration.VerificationDeadlineUtc, utcNow);
        }

        return token;
    }

    public async Task ReleasePendingSlugAsync(PlatformTenantRegistration registration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);

        if (await _slugs.FindAsync(registration.Slug, cancellationToken) is { } claim && claim.IsHeldBy(registration.Id))
        {
            _slugs.Remove(claim);
        }
    }
}
