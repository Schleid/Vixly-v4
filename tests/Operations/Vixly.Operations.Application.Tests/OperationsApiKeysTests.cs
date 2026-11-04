using Vixly.Operations.Application.Api;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsApiKeysTests.cs
//
// PURPOSE:
// Proves the company API keys, cursors and webhook signatures (Slice 10).
//
// RESPONSIBILITIES:
// - A key reads back into its id and secret; only the hash matches; a cursor reads back; the signature follows Standard Webhooks
//
// IMPORTANT NOTES:
// - Pure; no database
// =========================================================
public sealed class OperationsApiKeysTests
{
    [Fact]
    public void Key_ReadsBack_AndOnlyItsSecretMatchesTheHash()
    {
        (string keyId, string token, string hash, string hint) = OperationsApiKeys.Generate();
        Assert.True(OperationsApiKeys.TryRead(token, out string readId, out string secret));
        Assert.Equal(keyId, readId);
        Assert.True(OperationsApiKeys.Matches(secret, hash));
        Assert.EndsWith(hint, token, StringComparison.Ordinal);
        Assert.False(OperationsApiKeys.Matches(secret + "x", hash));
        Assert.DoesNotContain(secret, hash, StringComparison.Ordinal);
        Assert.False(OperationsApiKeys.TryRead("sk_" + token[3..], out _, out _));
        Assert.False(OperationsApiKeys.TryRead("ak_short", out _, out _));
        Assert.NotEqual(token, OperationsApiKeys.Generate().Token);
    }

    [Fact]
    public void Cursor_ReadsBack_AndAnythingElseIsTheStart()
    {
        DateTimeOffset at = new(2026, 10, 22, 9, 30, 15, 123, TimeSpan.Zero);
        Guid id = Guid.NewGuid();
        Assert.True(OperationsApiCursor.TryRead(OperationsApiCursor.Write(at, id), out DateTimeOffset readAt, out Guid readId));
        Assert.Equal((at, id), (readAt, readId));
        Assert.False(OperationsApiCursor.TryRead("not-a-cursor!", out _, out _));
        Assert.False(OperationsApiCursor.TryRead(null, out _, out _));
    }

    [Fact]
    public void Signature_IsHmacSha256_OverIdTimestampAndBody_WithTheSecretBytes()
    {
        // The Standard Webhooks example (secret, id, timestamp, body) and its published signature.
        string signature = OperationsWebhookSignature.Sign("whsec_MfKQ9r8GKYqrTwjUPD8ILPZIo2LaLaSw", "msg_p5jXN8AQM9LWM0D4loKWxJek", 1614265330,
            "{\"test\": 2432232314}");
        Assert.Equal("v1,g0hM9SsE+OTPJTGt/tmIKtSyZlE3uFJELVlNIOLJ1OE=", signature);
        Assert.StartsWith("whsec_", OperationsWebhookSignature.NewSecret(), StringComparison.Ordinal);
    }
}
