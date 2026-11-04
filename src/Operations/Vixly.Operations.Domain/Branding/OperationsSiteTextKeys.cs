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

    // Landing page marketing (owner, 2026-10-30): promise chips, story checklists and "why us" cards.
    public const int Chips = 3;

    public const int StoryPoints = 4;

    public const int WhyCards = 4;

    // The services with a delivery time in the key figures and a story panel.
    public static IReadOnlyList<string> StoryServices { get; } = new[] { "air", "ocean" };

    public static int? MaxLength(string key) => Limits.TryGetValue(key, out int max) ? max : null;

    private static Dictionary<string, int> Build()
    {
        Dictionary<string, int> limits = new(StringComparer.Ordinal)
        {
            [Headline] = 90,
            [Intro] = 300,
            [MetaDescription] = 160,
            [FooterAbout] = 300,
            ["eyebrow"] = 60,
            ["figure.value"] = 20,
            ["figure.label"] = 60,
            ["figure.since"] = 30,
            ["why.heading"] = 90,
            ["closing.title"] = 90,
            ["closing.text"] = 200
        };

        for (int chip = 1; chip <= Chips; chip++)
        {
            limits["chip." + chip] = 40;
        }

        // Not StoryServices: this runs while the static fields are being set, before that list exists.
        foreach (string service in new[] { "air", "ocean" })
        {
            limits["time." + service] = 30;
            limits["story." + service + ".title"] = 90;
            limits["story." + service + ".text"] = 300;

            for (int point = 1; point <= StoryPoints; point++)
            {
                limits["story." + service + ".point." + point] = 100;
            }
        }

        for (int card = 1; card <= WhyCards; card++)
        {
            limits["why." + card + ".title"] = 60;
            limits["why." + card + ".text"] = 200;
        }

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
