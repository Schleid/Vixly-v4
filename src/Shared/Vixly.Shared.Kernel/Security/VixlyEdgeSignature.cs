using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Vixly.Shared.Kernel.Security;

// =========================================================
// FILE: VixlyEdgeSignature.cs
//
// PURPOSE:
// Signs what a site sends to the Cloudflare Worker in front of the sites (maintenance windows from Control,
// company brands from Operations; owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Build the signed POST: the time in Unix milliseconds, and the hex HMAC-SHA256 of
//   "POST\n{path}\n{time}\n{body}" with the shared secret
// - Say whether a secret is usable (32 characters or more)
//
// IMPORTANT NOTES:
// - The Worker (build/edge/maintenance-worker/worker.js) checks the same text; change both together
// - The Worker refuses a message older than 5 minutes, or not newer than the last one it accepted
// - Spaces around the secret are ignored, as the Worker does (a value pasted in a panel)
// - Never puts the secret in an exception message
// =========================================================
public static class VixlyEdgeSignature
{
    public const string TimeHeader = "X-Vixly-Edge-Time";

    public const string SignatureHeader = "X-Vixly-Edge-Signature";

    public const int MinSecretLength = 32;

    public static bool IsUsable(string? secret)
    {
        return secret is not null && secret.Trim().Length >= MinSecretLength;
    }

    public static string Sign(string secret, string path, long unixMilliseconds, string body)
    {
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(body);

        string text = "POST\n" + path + "\n" + unixMilliseconds.ToString(CultureInfo.InvariantCulture) + "\n" + body;
        return Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret.Trim()), Encoding.UTF8.GetBytes(text)));
    }

    // The signed POST of a JSON body to the Worker's address.
    public static HttpRequestMessage Request(Uri address, string secret, string json, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(json);

        if (!IsUsable(secret))
        {
            throw new InvalidOperationException("The edge secret is missing or shorter than 32 characters.");
        }

        long time = utcNow.ToUnixTimeMilliseconds();
        HttpRequestMessage request = new(HttpMethod.Post, address)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add(TimeHeader, time.ToString(CultureInfo.InvariantCulture));
        request.Headers.Add(SignatureHeader, Sign(secret, address.AbsolutePath, time, json));
        return request;
    }
}
