namespace Vixly.Shared.Localization;

// =========================================================
// FILE: VixlyPhone.cs
//
// PURPOSE:
// Shows a stored phone number in international form, the way people read it: "+1 305-300-4026", "+509 4137-6609".
//
// RESPONSIBILITIES:
// - Find the country calling code: from the number ("+509..."), or from the country it belongs to when the
//   number was saved without one ("8555480504" for a US company)
// - Group the national digits for the most used countries, and in threes for the others
//
// IMPORTANT NOTES:
// - Display only (owner, 2026-10-25): what is saved does not change, and links (tel:) keep the stored number
// - Not telecom validation: a number that cannot be read is shown as it was saved
// =========================================================
public static class VixlyPhone
{
    private static readonly Dictionary<string, string> CallingCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AD"] = "376", ["AE"] = "971", ["AF"] = "93", ["AG"] = "1", ["AI"] = "1", ["AL"] = "355", ["AM"] = "374", ["AO"] = "244",
        ["AR"] = "54", ["AS"] = "1", ["AT"] = "43", ["AU"] = "61", ["AW"] = "297", ["AX"] = "358", ["AZ"] = "994", ["BA"] = "387",
        ["BB"] = "1", ["BD"] = "880", ["BE"] = "32", ["BF"] = "226", ["BG"] = "359", ["BH"] = "973", ["BI"] = "257", ["BJ"] = "229",
        ["BL"] = "590", ["BM"] = "1", ["BN"] = "673", ["BO"] = "591", ["BQ"] = "599", ["BR"] = "55", ["BS"] = "1", ["BT"] = "975",
        ["BW"] = "267", ["BY"] = "375", ["BZ"] = "501", ["CA"] = "1", ["CC"] = "61", ["CD"] = "243", ["CF"] = "236", ["CG"] = "242",
        ["CH"] = "41", ["CI"] = "225", ["CK"] = "682", ["CL"] = "56", ["CM"] = "237", ["CN"] = "86", ["CO"] = "57", ["CR"] = "506",
        ["CU"] = "53", ["CV"] = "238", ["CW"] = "599", ["CX"] = "61", ["CY"] = "357", ["CZ"] = "420", ["DE"] = "49", ["DJ"] = "253",
        ["DK"] = "45", ["DM"] = "1", ["DO"] = "1", ["DZ"] = "213", ["EC"] = "593", ["EE"] = "372", ["EG"] = "20", ["ER"] = "291",
        ["ES"] = "34", ["ET"] = "251", ["FI"] = "358", ["FJ"] = "679", ["FK"] = "500", ["FM"] = "691", ["FO"] = "298", ["FR"] = "33",
        ["GA"] = "241", ["GB"] = "44", ["GD"] = "1", ["GE"] = "995", ["GF"] = "594", ["GG"] = "44", ["GH"] = "233", ["GI"] = "350",
        ["GL"] = "299", ["GM"] = "220", ["GN"] = "224", ["GP"] = "590", ["GQ"] = "240", ["GR"] = "30", ["GT"] = "502", ["GU"] = "1",
        ["GW"] = "245", ["GY"] = "592", ["HK"] = "852", ["HN"] = "504", ["HR"] = "385", ["HT"] = "509", ["HU"] = "36", ["ID"] = "62",
        ["IE"] = "353", ["IL"] = "972", ["IM"] = "44", ["IN"] = "91", ["IO"] = "246", ["IQ"] = "964", ["IR"] = "98", ["IS"] = "354",
        ["IT"] = "39", ["JE"] = "44", ["JM"] = "1", ["JO"] = "962", ["JP"] = "81", ["KE"] = "254", ["KG"] = "996", ["KH"] = "855",
        ["KI"] = "686", ["KM"] = "269", ["KN"] = "1", ["KP"] = "850", ["KR"] = "82", ["KW"] = "965", ["KY"] = "1", ["KZ"] = "7",
        ["LA"] = "856", ["LB"] = "961", ["LC"] = "1", ["LI"] = "423", ["LK"] = "94", ["LR"] = "231", ["LS"] = "266", ["LT"] = "370",
        ["LU"] = "352", ["LV"] = "371", ["LY"] = "218", ["MA"] = "212", ["MC"] = "377", ["MD"] = "373", ["ME"] = "382", ["MF"] = "590",
        ["MG"] = "261", ["MH"] = "692", ["MK"] = "389", ["ML"] = "223", ["MM"] = "95", ["MN"] = "976", ["MO"] = "853", ["MP"] = "1",
        ["MQ"] = "596", ["MR"] = "222", ["MS"] = "1", ["MT"] = "356", ["MU"] = "230", ["MV"] = "960", ["MW"] = "265", ["MX"] = "52",
        ["MY"] = "60", ["MZ"] = "258", ["NA"] = "264", ["NC"] = "687", ["NE"] = "227", ["NF"] = "672", ["NG"] = "234", ["NI"] = "505",
        ["NL"] = "31", ["NO"] = "47", ["NP"] = "977", ["NR"] = "674", ["NU"] = "683", ["NZ"] = "64", ["OM"] = "968", ["PA"] = "507",
        ["PE"] = "51", ["PF"] = "689", ["PG"] = "675", ["PH"] = "63", ["PK"] = "92", ["PL"] = "48", ["PM"] = "508", ["PR"] = "1",
        ["PS"] = "970", ["PT"] = "351", ["PW"] = "680", ["PY"] = "595", ["QA"] = "974", ["RE"] = "262", ["RO"] = "40", ["RS"] = "381",
        ["RU"] = "7", ["RW"] = "250", ["SA"] = "966", ["SB"] = "677", ["SC"] = "248", ["SD"] = "249", ["SE"] = "46", ["SG"] = "65",
        ["SH"] = "290", ["SI"] = "386", ["SJ"] = "47", ["SK"] = "421", ["SL"] = "232", ["SM"] = "378", ["SN"] = "221", ["SO"] = "252",
        ["SR"] = "597", ["SS"] = "211", ["ST"] = "239", ["SV"] = "503", ["SX"] = "1", ["SY"] = "963", ["SZ"] = "268", ["TC"] = "1",
        ["TD"] = "235", ["TG"] = "228", ["TH"] = "66", ["TJ"] = "992", ["TK"] = "690", ["TL"] = "670", ["TM"] = "993", ["TN"] = "216",
        ["TO"] = "676", ["TR"] = "90", ["TT"] = "1", ["TV"] = "688", ["TW"] = "886", ["TZ"] = "255", ["UA"] = "380", ["UG"] = "256",
        ["UM"] = "1", ["US"] = "1", ["UY"] = "598", ["UZ"] = "998", ["VA"] = "39", ["VC"] = "1", ["VE"] = "58", ["VG"] = "1",
        ["VI"] = "1", ["VN"] = "84", ["VU"] = "678", ["WF"] = "681", ["WS"] = "685", ["XK"] = "383", ["YE"] = "967", ["YT"] = "262",
        ["ZA"] = "27", ["ZM"] = "260", ["ZW"] = "263",
    };

    private static readonly HashSet<string> KnownCodes = new(CallingCodes.Values, StringComparer.Ordinal);

    public static string International(string? stored, string? countryCode = null)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return string.Empty;
        }

        string raw = stored.Trim();
        string digits = string.Concat(raw.Where(char.IsAsciiDigit));

        if (digits.Length is < 7 or > 15)
        {
            return raw;
        }

        string? code = null;
        string national = digits;

        if (raw.StartsWith('+') || digits.StartsWith("00", StringComparison.Ordinal))
        {
            string number = raw.StartsWith('+') ? digits : digits[2..];
            code = Split(number, out national);
        }
        else if (countryCode is not null && CallingCodes.TryGetValue(countryCode.Trim(), out string? known))
        {
            // Saved with its code but no plus ("50941376609"), or as dialled at home ("0612345678", "8555480504").
            if (digits.StartsWith(known, StringComparison.Ordinal) && digits.Length - known.Length >= 7 && (known != "1" || digits.Length == 11))
            {
                code = known;
                national = digits[known.Length..];
            }
            else
            {
                code = known;
                national = known == "1" ? digits : digits.TrimStart('0');
            }
        }
        else if (digits.Length == 10 && digits[0] is not ('0' or '1'))
        {
            code = "1";
        }
        else if (digits.Length == 11 && digits[0] == '1')
        {
            code = "1";
            national = digits[1..];
        }

        return code is null || national.Length < 4 ? raw : "+" + code + " " + Group(code, national);
    }

    // The longest known calling code at the start of an international number.
    private static string? Split(string number, out string national)
    {
        for (int length = 3; length >= 1; length--)
        {
            if (number.Length > length + 4 && KnownCodes.Contains(number[..length]))
            {
                national = number[length..];
                return number[..length];
            }
        }

        national = number;
        return null;
    }

    private static string Group(string code, string national) => (code, national.Length) switch
    {
        ("1", 10) => $"{national[..3]}-{national[3..6]}-{national[6..]}",
        ("509", 8) => $"{national[..4]}-{national[4..]}",
        ("33", 9) or ("262", 9) or ("590", 9) or ("594", 9) or ("596", 9) => $"{national[..1]} {national[1..3]} {national[3..5]} {national[5..7]} {national[7..]}",
        ("52", 10) => $"{national[..2]} {national[2..6]} {national[6..]}",
        ("44", 10) => $"{national[..4]} {national[4..]}",
        ("34", 9) => $"{national[..3]} {national[3..5]} {national[5..7]} {national[7..]}",
        _ => Threes(national)
    };

    // 123 456 7890: threes, the last group taking the rest so none is a single digit.
    private static string Threes(string digits)
    {
        List<string> groups = new();
        int at = 0;

        while (digits.Length - at > 4)
        {
            groups.Add(digits.Substring(at, 3));
            at += 3;
        }

        groups.Add(digits[at..]);
        return string.Join(' ', groups);
    }
}
