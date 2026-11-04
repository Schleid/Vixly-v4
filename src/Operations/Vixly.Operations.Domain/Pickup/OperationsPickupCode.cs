using System.Security.Cryptography;

namespace Vixly.Operations.Domain.Pickup;

// =========================================================
// FILE: OperationsPickupCode.cs
//
// PURPOSE:
// The pickup code sent to the customer (Slice 7 step 2, R-5).
//
// RESPONSIBILITIES:
// - Make a six-digit code; say how long it works
//
// IMPORTANT NOTES:
// - Kept protected with the server's key (the application protects and checks it), never in clear
// =========================================================
public static class OperationsPickupCode
{
    public const int Length = 6;

    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    // A new code may be sent at most this often (review).
    public static readonly TimeSpan ResendAfter = TimeSpan.FromMinutes(2);

    public static string New() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
}
