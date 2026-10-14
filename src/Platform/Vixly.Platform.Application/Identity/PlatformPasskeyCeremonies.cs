using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Identity;

// =========================================================
// FILE: PlatformPasskeyCeremonies.cs
//
// PURPOSE:
// Starts and finishes passkey ceremonies for the passkey use
// cases.
//
// RESPONSIBILITIES:
// - Start: store the options under a new random token and give
//   the token back
// - Use: find the ceremony by token, check purpose, owner and
//   expiry, and mark it used
//
// IMPORTANT NOTES:
// - Used means used: a ceremony is spent even when the answer then
//   fails verification, so it cannot be tried twice
// - The caller commits; nothing here saves
// =========================================================
public sealed class PlatformPasskeyCeremonies
{
    private readonly IPlatformPasskeyCeremonyRepository _ceremonies;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly TimeProvider _timeProvider;

    public PlatformPasskeyCeremonies(
        IPlatformPasskeyCeremonyRepository ceremonies,
        IPlatformSecretGenerator secrets,
        TimeProvider timeProvider)
    {
        _ceremonies = ceremonies ?? throw new ArgumentNullException(nameof(ceremonies));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public string Start(PlatformPasskeyCeremonyPurpose purpose, Guid? platformUserId, string optionsJson)
    {
        PlatformSecret token = _secrets.NewToken();
        _ceremonies.Add(PlatformPasskeyCeremony.Start(token.Hash, purpose, platformUserId, optionsJson, _timeProvider.GetUtcNow()));

        return token.Value;
    }

    public async Task<PlatformPasskeyCeremony?> UseAsync(
        string? token,
        PlatformPasskeyCeremonyPurpose purpose,
        Guid? platformUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        PlatformPasskeyCeremony? ceremony = await _ceremonies.FindByTokenHashAsync(_secrets.HashToken(token), cancellationToken);

        if (ceremony is null || !ceremony.IsUsableFor(purpose, platformUserId, utcNow))
        {
            return null;
        }

        ceremony.Complete(purpose, platformUserId, utcNow);

        return ceremony;
    }
}
