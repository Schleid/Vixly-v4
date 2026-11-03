using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Web.Site;

// =========================================================
// FILE: OperationsSiteHome.cs
//
// PURPOSE:
// What every home page theme shows, read once: the services switched on, the questions answered, the real key figures.
//
// RESPONSIBILITIES:
// - List the services the company offers, in display order
// - List the questions with both a question and an answer
// - Count services, countries and locations from the company's own data
//
// IMPORTANT NOTES:
// - Shared by the Classic, Horizon and Air & Sea home pages (website themes, step 1); never invents a figure
// =========================================================
public static class OperationsSiteHome
{
    public static IReadOnlyList<(OperationsSiteServices Flag, string Code)> AllServices { get; } = new[]
    {
        (OperationsSiteServices.Mailbox, "mailbox"), (OperationsSiteServices.Consolidation, "consolidation"), (OperationsSiteServices.Air, "air"),
        (OperationsSiteServices.Ocean, "ocean"), (OperationsSiteServices.DoorToDoor, "door_to_door"), (OperationsSiteServices.Pickup, "pickup")
    };

    public static IReadOnlyList<string> Services(OperationsSitePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return AllServices.Where(item => page.Site.Settings.Services.HasFlag(item.Flag)).Select(item => item.Code).ToList();
    }

    public static IReadOnlyList<(string Question, string Answer)> Faq(OperationsSitePage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return page.Site.Settings.ShowFaq
            ? Enumerable.Range(1, OperationsSiteTextKeys.FaqItems)
                .Select(item => (page.Site.Text("faq." + item + ".question"), page.Site.Text("faq." + item + ".answer")))
                .Where(pair => pair.Item1 is not null && pair.Item2 is not null)
                .Select(pair => (pair.Item1!, pair.Item2!))
                .ToList()
            : Array.Empty<(string, string)>();
    }

    // Key figures from real data only; a zero is not shown (design section 5).
    public static IReadOnlyList<(int Value, string LabelKey)> Figures(OperationsSitePage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return new[]
        {
            (Services(page).Count, "site.figures.services"),
            (page.Site.Locations.Select(location => location.CountryCode).Distinct(StringComparer.Ordinal).Count(), "site.figures.countries"),
            (page.Site.Locations.Count, "site.figures.locations")
        }.Where(item => item.Item1 > 0).ToList();
    }

    // Landing page marketing (owner, 2026-10-30): the company's own texts of one kind, skipping the empty ones ("chip.1" to "chip.3").
    public static IReadOnlyList<string> Texts(OperationsSitePage page, string prefix, int count, string suffix = "")
    {
        ArgumentNullException.ThrowIfNull(page);
        return Enumerable.Range(1, count).Select(item => page.Site.Text(prefix + item + suffix)).OfType<string>().Where(text => text.Length > 0).ToList();
    }

    // The "why us" cards with both a title and a text.
    public static IReadOnlyList<(string Title, string Text)> WhyCards(OperationsSitePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return Enumerable.Range(1, OperationsSiteTextKeys.WhyCards)
            .Select(card => (page.Site.Text("why." + card + ".title"), page.Site.Text("why." + card + ".text")))
            .Where(pair => pair.Item1 is { Length: > 0 } && pair.Item2 is { Length: > 0 })
            .Select(pair => (pair.Item1!, pair.Item2!))
            .ToList();
    }

    // Where customers collect their packages: the public locations that are pickup points.
    public static IReadOnlyList<Vixly.Operations.Application.Site.OperationsPublicLocation> PickupPoints(OperationsSitePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.Site.Locations.Where(location => location.PickupPoint).ToList();
    }
}
