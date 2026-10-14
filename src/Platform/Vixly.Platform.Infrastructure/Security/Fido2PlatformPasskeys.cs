using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Infrastructure.Hosting;

namespace Vixly.Platform.Infrastructure.Security;

// =========================================================
// FILE: Fido2PlatformPasskeys.cs
//
// PURPOSE:
// WebAuthn adapter for staff passkeys, on Fido2NetLib
// (ADR-0006 section 2).
//
// RESPONSIBILITIES:
// - Take the relying party (host and origin) from
//   PlatformHosting:ConsoleBaseUrl
// - Build registration and sign-in options: discoverable
//   passkeys, user verification required, no attestation
// - Verify answers: origin, challenge, signature, user handle and
//   counter (Fido2NetLib), then return what the domain stores
//
// IMPORTANT NOTES:
// - The user handle is the user id, never the email (no personal
//   data on the device)
// - A failed verification is logged at Warning with its reason
//   and answers null (D-6)
// - Passkeys are bound to the console host: a host change needs
//   re-enrolment (ADR-0012)
// - Singleton: holds only configuration
// =========================================================
internal sealed class Fido2PlatformPasskeys : IPlatformPasskeys
{
    private const string RelyingPartyName = "Vixly Control";

    private static readonly Action<ILogger, string, Exception?> LogRefused =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1, "PasskeyRefused"), "Passkey answer refused: {Reason}");

    private readonly Fido2 _fido2;

    private readonly ILogger<Fido2PlatformPasskeys> _logger;

    public Fido2PlatformPasskeys(IOptions<PlatformHostingOptions> hosting, ILogger<Fido2PlatformPasskeys> logger)
    {
        ArgumentNullException.ThrowIfNull(hosting);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!Uri.TryCreate(hosting.Value.ConsoleBaseUrl, UriKind.Absolute, out Uri? console))
        {
            throw new InvalidOperationException($"{PlatformHostingOptions.SectionName}:ConsoleBaseUrl must be an absolute URL.");
        }

        _fido2 = new Fido2(
            new Fido2Configuration
            {
                RPID = console.Host,
                RPName = RelyingPartyName,
                Origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { console.GetLeftPart(UriPartial.Authority) }
            },
            null);
    }

    public string CreateRegistrationOptions(PlatformUser user, IReadOnlyCollection<byte[]> excludedCredentialIds)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(excludedCredentialIds);

        CredentialCreateOptions options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = new Fido2User
            {
                Id = user.Id.ToByteArray(),
                Name = user.Email.Value,
                DisplayName = user.FirstName + " " + user.LastName
            },
            ExcludeCredentials = excludedCredentialIds.Select(id => new PublicKeyCredentialDescriptor(id)).ToList(),
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Required
            },
            AttestationPreference = AttestationConveyancePreference.None
        });

        return options.ToJson();
    }

    public async Task<PlatformPasskeyCredential?> VerifyRegistrationAsync(
        string optionsJson,
        string attestationJson,
        CancellationToken cancellationToken)
    {
        try
        {
            AuthenticatorAttestationRawResponse? answer = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(attestationJson);

            if (answer is null)
            {
                LogRefused(_logger, "empty registration answer", null);
                return null;
            }

            RegisteredPublicKeyCredential credential = await _fido2.MakeNewCredentialAsync(
                new MakeNewCredentialParams
                {
                    AttestationResponse = answer,
                    OriginalOptions = CredentialCreateOptions.FromJson(optionsJson),
                    IsCredentialIdUniqueToUserCallback = (_, _) => Task.FromResult(true)
                },
                cancellationToken);

            return new PlatformPasskeyCredential(credential.Id, credential.PublicKey, credential.SignCount, credential.AaGuid);
        }
        catch (Exception exception) when (exception is Fido2VerificationException or JsonException or FormatException or ArgumentException)
        {
            LogRefused(_logger, "registration did not verify", exception);
            return null;
        }
    }

    public string CreateAssertionOptions(IReadOnlyCollection<byte[]> allowedCredentialIds)
    {
        ArgumentNullException.ThrowIfNull(allowedCredentialIds);

        return _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = allowedCredentialIds.Select(id => new PublicKeyCredentialDescriptor(id)).ToList(),
            UserVerification = UserVerificationRequirement.Required
        }).ToJson();
    }

    public byte[]? ReadCredentialId(string assertionJson)
    {
        try
        {
            AuthenticatorAssertionRawResponse? answer = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(assertionJson);

            return answer?.RawId is { Length: > 0 } rawId ? rawId : null;
        }
        catch (Exception exception) when (exception is JsonException or FormatException or ArgumentException)
        {
            LogRefused(_logger, "unreadable sign-in answer", exception);
            return null;
        }
    }

    public async Task<long?> VerifyAssertionAsync(
        string optionsJson,
        string assertionJson,
        PlatformUserPasskey passkey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(passkey);

        byte[] owner = passkey.PlatformUserId.ToByteArray();

        try
        {
            AuthenticatorAssertionRawResponse? answer = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(assertionJson);

            if (answer is null)
            {
                LogRefused(_logger, "empty sign-in answer", null);
                return null;
            }

            VerifyAssertionResult result = await _fido2.MakeAssertionAsync(
                new MakeAssertionParams
                {
                    AssertionResponse = answer,
                    OriginalOptions = AssertionOptions.FromJson(optionsJson),
                    StoredPublicKey = passkey.PublicKey,
                    StoredSignatureCounter = (uint)passkey.SignatureCounter,
                    IsUserHandleOwnerOfCredentialIdCallback = (owned, _) => Task.FromResult(
                        passkey.Matches(owned.CredentialId)
                        && (owned.UserHandle is null || owned.UserHandle.AsSpan().SequenceEqual(owner)))
                },
                cancellationToken);

            return result.SignCount;
        }
        catch (Exception exception) when (exception is Fido2VerificationException or JsonException or FormatException or ArgumentException)
        {
            LogRefused(_logger, "sign-in did not verify", exception);
            return null;
        }
    }
}
