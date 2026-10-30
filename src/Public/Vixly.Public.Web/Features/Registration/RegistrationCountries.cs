using System.Globalization;

namespace Vixly.Public.Web.Features.Registration;

// =========================================================
// FILE: RegistrationCountries.cs
//
// PURPOSE:
// The countries offered on the sign-up form, named in their own language.
//
// RESPONSIBILITIES:
// - List the country codes and their native names
//
// IMPORTANT NOTES:
// - Platform checks the code against its full list; this list only shapes the form
// =========================================================
public static class RegistrationCountries
{
    public static IReadOnlyList<string> Codes { get; } = new[] { "US", "CA", "MX", "PR", "DO", "HT", "JM", "GB", "FR", "BE", "CH", "ES", "DE" };

    public static string Name(string code)
    {
        try
        {
            return new RegionInfo(code).NativeName;
        }
        catch (ArgumentException)
        {
            return code;
        }
    }
}
