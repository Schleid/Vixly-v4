using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Infrastructure.Hosting;
using Vixly.Platform.Infrastructure.Security;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformPasskeyAdapterTests.cs
//
// PURPOSE:
// Proves the options the WebAuthn adapter gives the browser and
// that it refuses garbage quietly.
//
// RESPONSIBILITIES:
// - Relying party comes from ConsoleBaseUrl
// - Registration: discoverable, user verification required, the
//   user handle is the user id (not the email), known passkeys
//   excluded
// - Sign-in: user verification required; empty allow list for
//   discoverable sign-in
// - Malformed answers answer null instead of throwing
//
// IMPORTANT NOTES:
// - Full ceremonies with a real authenticator are proven in the
//   browser run (virtual authenticator)
// =========================================================
public sealed class PlatformPasskeyAdapterTests
{
    private readonly Fido2PlatformPasskeys _passkeys = new(
        Options.Create(new PlatformHostingOptions { ConsoleBaseUrl = "https://control.techcrafters.ltd" }),
        NullLogger<Fido2PlatformPasskeys>.Instance);

    [Fact]
    public void RegistrationOptions_AreDiscoverable_VerifiedAndFreeOfPersonalHandles()
    {
        PlatformUser user = PlatformUser.Invite(
            "Ada", "Lovelace", EmailAddress.Create("ada@example.com"), LanguageCode.En,
            PlatformUserAuthorityLevel.Standard, null, null, DateTimeOffset.UtcNow);

        using JsonDocument options = JsonDocument.Parse(_passkeys.CreateRegistrationOptions(user, new[] { new byte[] { 1, 2, 3 } }));
        JsonElement root = options.RootElement;

        Assert.Equal("control.techcrafters.ltd", root.GetProperty("rp").GetProperty("id").GetString());
        Assert.Equal("required", root.GetProperty("authenticatorSelection").GetProperty("residentKey").GetString());
        Assert.Equal("required", root.GetProperty("authenticatorSelection").GetProperty("userVerification").GetString());
        Assert.Equal(Base64Url(user.Id.ToByteArray()), root.GetProperty("user").GetProperty("id").GetString());
        Assert.Single(root.GetProperty("excludeCredentials").EnumerateArray());
        Assert.True(root.GetProperty("challenge").GetString()!.Length >= 22);
    }

    [Fact]
    public void SignInOptions_RequireUserVerification()
    {
        using JsonDocument options = JsonDocument.Parse(_passkeys.CreateAssertionOptions(Array.Empty<byte[]>()));

        Assert.Equal("required", options.RootElement.GetProperty("userVerification").GetString());
        Assert.Equal("control.techcrafters.ltd", options.RootElement.GetProperty("rpId").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"id\":\"%%%\"}")]
    public async Task MalformedAnswers_AreRefusedQuietly(string answer)
    {
        Assert.Null(_passkeys.ReadCredentialId(answer));
        Assert.Null(await _passkeys.VerifyRegistrationAsync(_passkeys.CreateAssertionOptions(Array.Empty<byte[]>()), answer, CancellationToken.None));
    }

    private static string Base64Url(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
