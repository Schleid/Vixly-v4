using Vixly.Shared.Kernel.Security;

namespace Vixly.Shared.Kernel.Tests;

// =========================================================
// FILE: VixlyEdgeSignatureTests.cs
//
// PURPOSE:
// Proves the sites sign what they send to the Cloudflare Worker the way the Worker checks it.
//
// RESPONSIBILITIES:
// - The known value, the headers, the trimmed secret and the refusal of a short one
//
// IMPORTANT NOTES:
// - The same value is checked by the Worker's own tests (build/edge/maintenance-worker/worker.test.mjs)
// =========================================================
public sealed class VixlyEdgeSignatureTests
{
    private static readonly string Secret = new('k', 32);

    [Fact]
    public void Sign_MatchesTheWorker()
    {
        Assert.Equal("bf9cb6c4634a9b5fe9e38df0c005f95c390c093a7b0a7433738ae32b55898f0d",
            VixlyEdgeSignature.Sign(Secret, "/__edge/brand", 1700000000000, "{\"slug\":\"galeo\"}"));
        Assert.Equal(VixlyEdgeSignature.Sign(Secret, "/__edge/brand", 1700000000000, "{}"), VixlyEdgeSignature.Sign("  " + Secret + "\n", "/__edge/brand", 1700000000000, "{}"));
    }

    [Fact]
    public async Task Request_CarriesTheTimeAndTheSignatureOfItsPathAndBody()
    {
        DateTimeOffset now = DateTimeOffset.FromUnixTimeMilliseconds(1700000000000);

        using HttpRequestMessage request = VixlyEdgeSignature.Request(new Uri("https://api.vixly.ltd/__edge/brand"), Secret, "{\"slug\":\"galeo\"}", now);

        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("1700000000000", request.Headers.GetValues(VixlyEdgeSignature.TimeHeader).Single());
        Assert.Equal("bf9cb6c4634a9b5fe9e38df0c005f95c390c093a7b0a7433738ae32b55898f0d", request.Headers.GetValues(VixlyEdgeSignature.SignatureHeader).Single());
        Assert.Equal("{\"slug\":\"galeo\"}", await request.Content!.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("short", false)]
    [InlineData("                                        ", false)]
    [InlineData("0123456789abcdef0123456789abcdef", true)]
    public void IsUsable_Needs32Characters(string? secret, bool usable)
    {
        Assert.Equal(usable, VixlyEdgeSignature.IsUsable(secret));
    }

    [Fact]
    public void Request_RefusesAMissingSecret_WithoutShowingIt()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => VixlyEdgeSignature.Request(new Uri("https://api.vixly.ltd/__edge/brand"), "short-secret", "{}", DateTimeOffset.UtcNow));
        Assert.DoesNotContain("short-secret", error.Message, StringComparison.Ordinal);
    }
}
