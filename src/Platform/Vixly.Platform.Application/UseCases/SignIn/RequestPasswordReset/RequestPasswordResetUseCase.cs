using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.UseCases.SignIn.RequestPasswordReset;

// =========================================================
// FILE: RequestPasswordResetUseCase.cs
//
// PURPOSE:
// A staff member who forgot their password asks for a reset link.
//
// RESPONSIBILITIES:
// - Throttle: one link per email per 15 minutes, 5 requests
//   per client IP per hour (V3 behaviour plus an IP limit)
// - Send a link only to an active account; older unused
//   links are revoked
// - Log every request, including unknown emails
//
// IMPORTANT NOTES:
// - Same answer and same work for known and unknown emails,
//   except the email itself
// =========================================================
public sealed class RequestPasswordResetUseCase
{
    private const int IpLimit = 5;

    private static readonly TimeSpan EmailWindow = TimeSpan.FromMinutes(15);

    private static readonly TimeSpan IpWindow = TimeSpan.FromHours(1);

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformAccountTokenRepository _tokens;

    private readonly IPlatformPasswordResetRequestRepository _requests;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformIdentityMailer _mailer;

    private readonly IPlatformRequestMetadata _metadata;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RequestPasswordResetUseCase(
        IPlatformUserRepository users,
        IPlatformAccountTokenRepository tokens,
        IPlatformPasswordResetRequestRepository requests,
        IPlatformSecretGenerator secrets,
        IPlatformIdentityMailer mailer,
        IPlatformRequestMetadata metadata,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _requests = requests ?? throw new ArgumentNullException(nameof(requests));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RequestPasswordResetResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        RequestPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (!EmailAddress.TryCreate(request.Email, out EmailAddress? email))
        {
            return RequestPasswordResetResponse.Success();
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string? ip = _metadata.ClientIp;

        bool throttled =
            await _requests.CountSinceAsync(email!, utcNow - EmailWindow, cancellationToken) > 0
            || (ip is not null && await _requests.CountFromIpSinceAsync(ip, utcNow - IpWindow, cancellationToken) >= IpLimit);

        PlatformUser? user = throttled ? null : await _users.FindByEmailAsync(email!, cancellationToken);
        bool sendLink = user is not null && user.Account.IsActive;
        PlatformSecret? token = null;

        if (sendLink)
        {
            foreach (PlatformAccountToken older in await _tokens.ListUnusedAsync(
                user!.Id,
                PlatformAccountTokenPurpose.PasswordReset,
                cancellationToken))
            {
                older.Revoke(utcNow);
            }

            token = _secrets.NewToken();
            _tokens.Add(PlatformAccountToken.Issue(user.Id, PlatformAccountTokenPurpose.PasswordReset, token.Hash, utcNow));
            _audit.Record(null, "platform.auth.password_reset_requested", null, nameof(PlatformUser), user.Id);
        }

        _requests.Add(PlatformPasswordResetRequest.Record(email!, ip, sendLink, utcNow));

        if (await _unitOfWork.SaveChangesAsync(cancellationToken) && sendLink)
        {
            await _mailer.SendPasswordResetAsync(user!, token!.Value, cancellationToken);
        }

        return RequestPasswordResetResponse.Success();
    }
}
