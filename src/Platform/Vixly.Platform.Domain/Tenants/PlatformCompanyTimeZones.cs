namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformCompanyTimeZones.cs
//
// PURPOSE:
// The time zone a new company starts with (owner, 2026-11-02).
//
// RESPONSIBILITIES:
// - Take the time zone of the browser that signed up, when it is a valid IANA id
// - Otherwise use the main time zone of the company's state or province, or of its country
//
// IMPORTANT NOTES:
// - Never refuses a sign-up: a missing or unknown zone falls back to the address, then UTC
// - A state that spans two zones gets its main one (Florida: America/New_York); the owner can change it in settings
// - The tables cover the countries Vixly serves; an unknown country starts on UTC
// =========================================================
public static class PlatformCompanyTimeZones
{
    private static readonly IReadOnlyDictionary<string, string> Countries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["US"] = "America/New_York",
        ["CA"] = "America/Toronto",
        ["MX"] = "America/Mexico_City",
        ["PR"] = "America/Puerto_Rico",
        ["DO"] = "America/Santo_Domingo",
        ["HT"] = "America/Port-au-Prince",
        ["JM"] = "America/Jamaica",
        ["BS"] = "America/Nassau",
        ["TC"] = "America/Grand_Turk",
        ["KY"] = "America/Cayman",
        ["CU"] = "America/Havana",
        ["BM"] = "Atlantic/Bermuda",
        ["VI"] = "America/St_Thomas",
        ["VG"] = "America/Tortola",
        ["AG"] = "America/Antigua",
        ["KN"] = "America/St_Kitts",
        ["DM"] = "America/Dominica",
        ["LC"] = "America/St_Lucia",
        ["VC"] = "America/St_Vincent",
        ["GD"] = "America/Grenada",
        ["BB"] = "America/Barbados",
        ["TT"] = "America/Port_of_Spain",
        ["AW"] = "America/Aruba",
        ["CW"] = "America/Curacao",
        ["SX"] = "America/Lower_Princes",
        ["GP"] = "America/Guadeloupe",
        ["MQ"] = "America/Martinique",
        ["GF"] = "America/Cayenne",
        ["GY"] = "America/Guyana",
        ["SR"] = "America/Paramaribo",
        ["BZ"] = "America/Belize",
        ["GT"] = "America/Guatemala",
        ["SV"] = "America/El_Salvador",
        ["HN"] = "America/Tegucigalpa",
        ["NI"] = "America/Managua",
        ["CR"] = "America/Costa_Rica",
        ["PA"] = "America/Panama",
        ["CO"] = "America/Bogota",
        ["VE"] = "America/Caracas",
        ["EC"] = "America/Guayaquil",
        ["PE"] = "America/Lima",
        ["BR"] = "America/Sao_Paulo",
        ["GB"] = "Europe/London",
        ["IE"] = "Europe/Dublin",
        ["FR"] = "Europe/Paris",
        ["BE"] = "Europe/Brussels",
        ["NL"] = "Europe/Amsterdam",
        ["LU"] = "Europe/Luxembourg",
        ["CH"] = "Europe/Zurich",
        ["DE"] = "Europe/Berlin",
        ["ES"] = "Europe/Madrid",
        ["PT"] = "Europe/Lisbon",
        ["IT"] = "Europe/Rome"
    };

    // US states and territories by postal code; a state over two zones keeps its main one.
    private static readonly IReadOnlyDictionary<string, string> UsStates = Zones(
        ("America/New_York", "CT DE DC FL GA ME MD MA NH NJ NY NC OH PA RI SC VT VA WV"),
        ("America/Detroit", "MI"),
        ("America/Indiana/Indianapolis", "IN"),
        ("America/Kentucky/Louisville", "KY"),
        ("America/Chicago", "AL AR IL IA KS LA MN MS MO NE ND OK SD TN TX WI"),
        ("America/Denver", "CO MT NM UT WY"),
        ("America/Boise", "ID"),
        ("America/Phoenix", "AZ"),
        ("America/Los_Angeles", "CA NV OR WA"),
        ("America/Anchorage", "AK"),
        ("Pacific/Honolulu", "HI"),
        ("America/Puerto_Rico", "PR"),
        ("America/St_Thomas", "VI"),
        ("Pacific/Guam", "GU MP"),
        ("Pacific/Pago_Pago", "AS"));

    private static readonly IReadOnlyDictionary<string, string> UsStateNames = Names(UsStates, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Alabama"] = "AL", ["Alaska"] = "AK", ["Arizona"] = "AZ", ["Arkansas"] = "AR", ["California"] = "CA", ["Colorado"] = "CO",
        ["Connecticut"] = "CT", ["Delaware"] = "DE", ["District of Columbia"] = "DC", ["Florida"] = "FL", ["Georgia"] = "GA",
        ["Hawaii"] = "HI", ["Idaho"] = "ID", ["Illinois"] = "IL", ["Indiana"] = "IN", ["Iowa"] = "IA", ["Kansas"] = "KS",
        ["Kentucky"] = "KY", ["Louisiana"] = "LA", ["Maine"] = "ME", ["Maryland"] = "MD", ["Massachusetts"] = "MA",
        ["Michigan"] = "MI", ["Minnesota"] = "MN", ["Mississippi"] = "MS", ["Missouri"] = "MO", ["Montana"] = "MT",
        ["Nebraska"] = "NE", ["Nevada"] = "NV", ["New Hampshire"] = "NH", ["New Jersey"] = "NJ", ["New Mexico"] = "NM",
        ["New York"] = "NY", ["North Carolina"] = "NC", ["North Dakota"] = "ND", ["Ohio"] = "OH", ["Oklahoma"] = "OK",
        ["Oregon"] = "OR", ["Pennsylvania"] = "PA", ["Rhode Island"] = "RI", ["South Carolina"] = "SC", ["South Dakota"] = "SD",
        ["Tennessee"] = "TN", ["Texas"] = "TX", ["Utah"] = "UT", ["Vermont"] = "VT", ["Virginia"] = "VA", ["Washington"] = "WA",
        ["West Virginia"] = "WV", ["Wisconsin"] = "WI", ["Wyoming"] = "WY", ["Puerto Rico"] = "PR", ["Guam"] = "GU",
        ["U.S. Virgin Islands"] = "VI", ["US Virgin Islands"] = "VI", ["American Samoa"] = "AS", ["Northern Mariana Islands"] = "MP"
    });

    private static readonly IReadOnlyDictionary<string, string> CaProvinces = Zones(
        ("America/Toronto", "ON QC"),
        ("America/Halifax", "NS NB PE"),
        ("America/St_Johns", "NL"),
        ("America/Winnipeg", "MB"),
        ("America/Regina", "SK"),
        ("America/Edmonton", "AB NT"),
        ("America/Vancouver", "BC"),
        ("America/Whitehorse", "YT"),
        ("America/Iqaluit", "NU"));

    private static readonly IReadOnlyDictionary<string, string> CaProvinceNames = Names(CaProvinces, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Ontario"] = "ON", ["Quebec"] = "QC", ["Québec"] = "QC", ["Nova Scotia"] = "NS", ["New Brunswick"] = "NB",
        ["Prince Edward Island"] = "PE", ["Newfoundland and Labrador"] = "NL", ["Manitoba"] = "MB", ["Saskatchewan"] = "SK",
        ["Alberta"] = "AB", ["British Columbia"] = "BC", ["Yukon"] = "YT", ["Northwest Territories"] = "NT", ["Nunavut"] = "NU"
    });

    public static string Choose(string? browserTimeZone, PlatformPostalAddress? address)
    {
        string? browser = Valid(browserTimeZone);

        if (browser is not null && !string.Equals(browser, PlatformTimeZones.Utc, StringComparison.Ordinal))
        {
            return browser;
        }

        return ForAddress(address);
    }

    public static string ForAddress(PlatformPostalAddress? address)
    {
        if (address is null)
        {
            return PlatformTimeZones.Utc;
        }

        string country = address.CountryCode.Trim();
        string? state = address.StateProvince?.Trim().TrimEnd('.');
        string? zone = null;

        if (!string.IsNullOrEmpty(state))
        {
            zone = country.ToUpperInvariant() switch
            {
                "US" => UsStateNames.GetValueOrDefault(state),
                "CA" => CaProvinceNames.GetValueOrDefault(state),
                _ => null
            };
        }

        zone ??= Countries.GetValueOrDefault(country);
        return Valid(zone) ?? PlatformTimeZones.Utc;
    }

    private static string? Valid(string? zone)
    {
        try
        {
            return string.IsNullOrWhiteSpace(zone) ? null : PlatformTimeZones.Normalize(zone);
        }
        catch (Vixly.Shared.Kernel.Domain.DomainException)
        {
            return null;
        }
    }

    private static Dictionary<string, string> Zones(params (string Zone, string Codes)[] groups)
    {
        Dictionary<string, string> zones = new(StringComparer.OrdinalIgnoreCase);

        foreach ((string zone, string codes) in groups)
        {
            foreach (string code in codes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                zones[code] = zone;
            }
        }

        return zones;
    }

    // Codes and full names both find the zone ("FL" and "Florida").
    private static Dictionary<string, string> Names(IReadOnlyDictionary<string, string> byCode, Dictionary<string, string> names)
    {
        Dictionary<string, string> all = new(byCode, StringComparer.OrdinalIgnoreCase);

        foreach ((string name, string code) in names)
        {
            all[name] = byCode[code];
        }

        return all;
    }
}
