using System.Globalization;
using System.Text;

namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsApiCursor.cs
//
// PURPOSE:
// The opaque place a page of the company API ends at (Slice 10).
//
// RESPONSIBILITIES:
// - Write and read the place (time and id) as a short URL-safe string
//
// IMPORTANT NOTES:
// - Anything that does not read back is treated as the start
// =========================================================
public static class OperationsApiCursor
{
    public static string Write(DateTimeOffset createdUtc, Guid id) =>
        Convert.ToBase64String(Encoding.ASCII.GetBytes(createdUtc.UtcTicks.ToString(CultureInfo.InvariantCulture) + ":" + id.ToString("N")))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static bool TryRead(string? cursor, out DateTimeOffset createdUtc, out Guid id)
    {
        (createdUtc, id) = (DateTimeOffset.MinValue, Guid.Empty);

        if (string.IsNullOrWhiteSpace(cursor) || cursor.Length > 80)
        {
            return false;
        }

        try
        {
            string padded = cursor.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - (padded.Length % 4)) % 4);
            string[] parts = Encoding.ASCII.GetString(Convert.FromBase64String(padded)).Split(':');

            if (parts.Length == 2 && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out long ticks)
                && ticks >= DateTimeOffset.MinValue.UtcTicks && ticks <= DateTimeOffset.MaxValue.UtcTicks && Guid.TryParseExact(parts[1], "N", out Guid parsed))
            {
                (createdUtc, id) = (new DateTimeOffset(ticks, TimeSpan.Zero), parsed);
                return true;
            }
        }
        catch (FormatException)
        {
        }

        return false;
    }
}
