using System.Globalization;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformEmployeeNumber.cs
//
// PURPOSE:
// The employee number of a Platform (Control) staff member: TC- and a running number.
//
// RESPONSIBILITIES:
// - Format a number from the database sequence (TC-0001, TC-0002 ... TC-10000)
//
// IMPORTANT NOTES:
// - Owner, 2026-10-25: numbers are given automatically, never typed. TC is for Tech Crafters
//   (VX was too close to the product name)
// - Given once, at invitation, and never reused, even when the person leaves
// =========================================================
public static class PlatformEmployeeNumber
{
    public const string Prefix = "TC-";

    public static string Format(long number)
    {
        if (number < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "An employee number starts at 1.");
        }

        return Prefix + number.ToString("0000", CultureInfo.InvariantCulture);
    }
}
