using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Vixly.Platform.Infrastructure.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedSignature.cs
//
// PURPOSE:
// Signs a sales feed POST as contract version 1 requires.
//
// RESPONSIBILITIES:
// - X-TC-Signature: "v1=" + lower-case hex of HMAC-SHA256 over the UTF-8 bytes of "<timestamp>.<raw body>"
// - X-TC-Timestamp: Unix seconds of the attempt
//
// IMPORTANT NOTES:
// - The secret is used as UTF-8 bytes; TechOS refuses a timestamp more than 300 seconds off
// - A fresh timestamp for each POST
// =========================================================
public static class PlatformSalesFeedSignature
{
    public const string SourceHeader = "X-TC-Source";

    public const string ContractHeader = "X-TC-Contract";

    public const string TimestampHeader = "X-TC-Timestamp";

    public const string SignatureHeader = "X-TC-Signature";

    public static string Timestamp(DateTimeOffset utcNow)
    {
        return utcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
    }

    public static string Sign(string secret, string timestamp, string body)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);
        ArgumentNullException.ThrowIfNull(timestamp);
        ArgumentNullException.ThrowIfNull(body);

        byte[] hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(timestamp + "." + body));
        return "v1=" + Convert.ToHexStringLower(hash);
    }
}
