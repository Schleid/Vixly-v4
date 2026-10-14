using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Identity;

// =========================================================
// FILE: PlatformFirstSuperUserSetup.cs
//
// PURPOSE:
// Creates the very first Platform staff account, a SuperUser,
// on an empty database.
//
// RESPONSIBILITIES:
// - Refuse when any staff user already exists
// - Create the user as Invited, with a 72-hour invitation
//   link, and audit it
// - Return the raw invitation token to the operator who ran
//   the setup
//
// IMPORTANT NOTES:
// - Not a console use case: it is run once from the host's
//   command line by the person installing V4
//   (Platform.Web --setup-first-superuser ...)
// - No password is ever set or shown; the new SuperUser
//   accepts the invitation and enrols a second factor
// - Returns the token, or an Error
// =========================================================
public sealed class PlatformFirstSuperUserSetup
{
    private readonly IPlatformUserRepository _users;

    private readonly IPlatformAccountTokenRepository _tokens;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlatformFirstSuperUserSetup(
        IPlatformUserRepository users,
        IPlatformAccountTokenRepository tokens,
        IPlatformSecretGenerator secrets,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<(string? InvitationToken, Error? Error)> RunAsync(
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken)
    {
        if (await _users.AnyAsync(cancellationToken))
        {
            return (null, PlatformAuthErrors.SetupAlreadyDone);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        PlatformUser user = PlatformUser.Invite(
            firstName,
            lastName,
            EmailAddress.Create(email),
            LanguageCode.Default,
            PlatformUserAuthorityLevel.SuperUser,
            null,
            null,
            utcNow);

        PlatformSecret token = _secrets.NewToken();
        _users.Add(user);
        _tokens.Add(PlatformAccountToken.Issue(user.Id, PlatformAccountTokenPurpose.Invitation, token.Hash, utcNow));
        _audit.Record(null, "platform.setup.first_super_user_invited", null, nameof(PlatformUser), user.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? (token.Value, null)
            : (null, PlatformAuthErrors.SetupAlreadyDone);
    }
}
