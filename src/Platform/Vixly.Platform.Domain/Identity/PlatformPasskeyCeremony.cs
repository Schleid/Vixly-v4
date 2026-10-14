using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformPasskeyCeremony.cs
//
// PURPOSE:
// The server-side state of one WebAuthn ceremony: the options
// the browser was given, kept until the answer comes back.
//
// RESPONSIBILITIES:
// - Bind the ceremony to its purpose and, except for sign-in, to
//   one user
// - Expire after 5 minutes
// - Be used once, so a captured answer cannot be replayed
//
// IMPORTANT NOTES:
// - Aggregate root
// - The options hold the random challenge the answer must sign;
//   keeping them here, not in the browser, is what makes the
//   check trustworthy
// - Sign-in ceremonies have no user: the passkey itself says who
//   is signing in (discoverable credential)
// - The raw token is held by the caller; the table holds its
//   hash
// =========================================================
public sealed class PlatformPasskeyCeremony : AuditableEntity
{
    public const int OptionsMaxLength = 16000;

    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private PlatformPasskeyCeremony()
    {
        TokenHash = string.Empty;
        OptionsJson = string.Empty;
    }

    private PlatformPasskeyCeremony(
        string tokenHash,
        PlatformPasskeyCeremonyPurpose purpose,
        Guid? platformUserId,
        string optionsJson,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        TokenHash = tokenHash;
        Purpose = purpose;
        PlatformUserId = platformUserId;
        OptionsJson = optionsJson;
        ExpiresUtc = createdUtc.Add(Lifetime);
    }

    public string TokenHash { get; private set; }

    public PlatformPasskeyCeremonyPurpose Purpose { get; private set; }

    public Guid? PlatformUserId { get; private set; }

    public string OptionsJson { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public DateTimeOffset? CompletedUtc { get; private set; }

    // =====================================================
    // METHOD: START
    // =====================================================
    public static PlatformPasskeyCeremony Start(
        string tokenHash,
        PlatformPasskeyCeremonyPurpose purpose,
        Guid? platformUserId,
        string optionsJson,
        DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(purpose)
            || string.IsNullOrWhiteSpace(optionsJson)
            || optionsJson.Length > OptionsMaxLength
            || (purpose == PlatformPasskeyCeremonyPurpose.SignIn) != (platformUserId is null)
            || platformUserId == Guid.Empty)
        {
            throw new DomainException(PlatformIdentityErrors.PasskeyCeremonyInvalid);
        }

        return new PlatformPasskeyCeremony(PlatformTokenHash.Require(tokenHash), purpose, platformUserId, optionsJson, utcNow);
    }

    // =====================================================
    // METHOD: USE
    // =====================================================
    public bool IsUsableFor(PlatformPasskeyCeremonyPurpose purpose, Guid? platformUserId, DateTimeOffset utcNow)
    {
        return CompletedUtc is null
            && utcNow < ExpiresUtc
            && Purpose == purpose
            && PlatformUserId == platformUserId;
    }

    public void Complete(PlatformPasskeyCeremonyPurpose purpose, Guid? platformUserId, DateTimeOffset utcNow)
    {
        if (!IsUsableFor(purpose, platformUserId, utcNow))
        {
            throw new DomainException(PlatformIdentityErrors.PasskeyCeremonyInvalid);
        }

        CompletedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
