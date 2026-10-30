using System.Text.RegularExpressions;

namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsSiteTextKeys.cs
//
// PURPOSE:
// The texts a tenant may write for its site, with their limits (Slice 1B).
//
// RESPONSIBILITIES:
// - List every key and its maximum length
//
// IMPORTANT NOTES:
// - A key not set by the tenant falls back to the default text of the page
// =========================================================
public static class OperationsSiteTextKeys
{
    public const string Headline = "headline";

    public const string Intro = "intro";

    public const string MetaDescription = "meta_description";

    public const string FooterAbout = "footer_about";

    private static readonly Dictionary<string, int> Limits = Build();

    public static IReadOnlyDictionary<string, int> All => Limits;

    public static IReadOnlyList<string> ServiceCodes { get; } = new[] { "air", "ocean", "consolidation", "mailbox", "door_to_door", "pickup" };

    public const int Steps = 4;

    public const int FaqItems = 6;

    public static int? MaxLength(string key) => Limits.TryGetValue(key, out int max) ? max : null;

    private static Dictionary<string, int> Build()
    {
        Dictionary<string, int> limits = new(StringComparer.Ordinal)
        {
            [Headline] = 90,
            [Intro] = 300,
            [MetaDescription] = 160,
            [FooterAbout] = 300
        };

        foreach (string service in new[] { "air", "ocean", "consolidation", "mailbox", "door_to_door", "pickup" })
        {
            limits["service." + service] = 200;
        }

        for (int step = 1; step <= Steps; step++)
        {
            limits["step." + step + ".title"] = 60;
            limits["step." + step + ".text"] = 200;
        }

        for (int item = 1; item <= FaqItems; item++)
        {
            limits["faq." + item + ".question"] = 150;
            limits["faq." + item + ".answer"] = 600;
        }

        return limits;
    }
}
