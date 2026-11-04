using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Vixly.Operations.Infrastructure.Platform;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsServiceTokenSignerTests.cs
//
// PURPOSE:
// Proves the tokens Operations shows Platform (Slice 0, D-F3, ADR-0014).
//
// RESPONSIBILITIES:
// - Sign as operations for Platform, 60 seconds, unique id
// - Verify with the public key
//
// IMPORTANT NOTES:
// - No network
// =========================================================
public sealed class OperationsServiceTokenSignerTests
{
    [Fact]
    public void Token_IsSignedForPlatform_AndShortLived()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using OperationsServiceTokenSigner signer = new(
            Options.Create(new OperationsPlatformOptions { CallerKeyId = "k1", CallerPrivateKey = Convert.ToBase64String(key.ExportPkcs8PrivateKey()) }),
            TimeProvider.System);
        Guid tenant = Guid.NewGuid();

        string first = signer.Sign(tenant);
        string second = signer.Sign(null);
        string[] parts = first.Split('.');
        using JsonDocument claims = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));

        Assert.True(key.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), Base64Url.DecodeFromChars(parts[2]), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        Assert.Equal("operations", claims.RootElement.GetProperty("iss").GetString());
        Assert.Equal("vixly-platform-internal", claims.RootElement.GetProperty("aud").GetString());
        Assert.Equal(60, claims.RootElement.GetProperty("exp").GetInt64() - claims.RootElement.GetProperty("iat").GetInt64());
        Assert.Equal(tenant.ToString("D"), claims.RootElement.GetProperty("tid").GetString());
        Assert.NotEqual(first.Split('.')[1], second.Split('.')[1]);
    }

    [Fact]
    public void ActingUser_TravelsAsClaims_PlatformReads()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using OperationsServiceTokenSigner signer = new(
            Options.Create(new OperationsPlatformOptions { CallerKeyId = "k1", CallerPrivateKey = Convert.ToBase64String(key.ExportPkcs8PrivateKey()) }),
            TimeProvider.System);
        Guid tenant = Guid.NewGuid();
        Guid user = Guid.NewGuid();

        string token = signer.Sign(tenant, new Vixly.Operations.Application.Ports.OperationsPlatformActor(tenant, user, "Grace Hopper", "owner", true, "203.0.113.9", "Browser"));
        using JsonDocument claims = JsonDocument.Parse(Base64Url.DecodeFromChars(token.Split('.')[1]));

        Assert.Equal(user.ToString("D"), claims.RootElement.GetProperty("sub").GetString());
        Assert.Equal("Grace Hopper", claims.RootElement.GetProperty("name").GetString());
        Assert.Equal("owner", claims.RootElement.GetProperty("role").GetString());
        Assert.True(claims.RootElement.GetProperty("stepup").GetBoolean());
        Assert.Equal("203.0.113.9", claims.RootElement.GetProperty("ip").GetString());
        Assert.Equal("Browser", claims.RootElement.GetProperty("ua").GetString());
    }

    [Theory]
    [InlineData("platform.licensing.no_free_seat", "ops.devices.no_free_seat")]
    [InlineData("platform.licensing.link_invalid", "ops.devices.link_invalid")]
    [InlineData("platform.common.step_up_required", "ops.devices.not_allowed")]
    [InlineData("platform.licensing.registration_not_ready", "ops.devices.not_ready")]
    [InlineData("platform.something.new", "ops.devices.refused")]
    public void PlatformRefusals_BecomeOperationsCodes(string platform, string operations)
    {
        Assert.Equal(operations, Vixly.Operations.Infrastructure.Platform.OperationsDeviceClient.Translate(platform).Code);
    }

    [Fact]
    public void NoKey_IsOff()
    {
        using OperationsServiceTokenSigner signer = new(Options.Create(new OperationsPlatformOptions()), TimeProvider.System);

        Assert.False(signer.Enabled);
        Assert.Throws<InvalidOperationException>(() => signer.Sign(null));
    }

    [Fact]
    public void WrongCurve_IsRefused()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP384);

        Assert.Throws<InvalidOperationException>(() => new OperationsServiceTokenSigner(
            Options.Create(new OperationsPlatformOptions { CallerKeyId = "k1", CallerPrivateKey = Convert.ToBase64String(key.ExportPkcs8PrivateKey()) }),
            TimeProvider.System));
    }
}
