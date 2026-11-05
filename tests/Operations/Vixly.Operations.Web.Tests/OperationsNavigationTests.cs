using Vixly.Operations.Web.Components.Editor;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsNavigationTests.cs
//
// PURPOSE:
// Proves the chapters of the staff menu and where the setup pages sit (owner, 2026-11-04).
//
// RESPONSIBILITIES:
// - Settings is its own chapter, just above Technical, with every page set up once
// - Staff, Roles and Company stay in Back Office; every page is in the menu once
//
// IMPORTANT NOTES:
// - Reads the menu list itself; which items a person sees is proven by the HTTP tests
// =========================================================
public sealed class OperationsNavigationTests
{
    [Fact]
    public void Settings_IsItsOwnChapter_JustAboveTechnical()
    {
        Assert.Equal(new[] { "front_office", "warehouse_operations", "back_office", "support", "settings", "technical" },
            OperationsNavigation.Groups.Select(group => group.Key));

        Assert.Equal(new[]
            {
                "settings", "invoice_settings", "payment_settings", "credit_settings", "pickup_settings", "delivery_settings",
                "bundle_settings", "event_settings", "inspection_setup", "report_settings"
            },
            Items("settings"));
    }

    [Fact]
    public void Staff_Roles_AndCompany_StayInBackOffice_AndEveryPageIsListedOnce()
    {
        Assert.Equal(new[] { "staff", "roles", "company" }, Items("back_office").TakeLast(3));
        Assert.DoesNotContain(Items("back_office"), section => section.EndsWith("_settings", StringComparison.Ordinal) || section is "settings" or "inspection_setup");

        string[] urls = OperationsNavigation.Groups.SelectMany(group => group.Items).Select(item => item.Url).ToArray();
        Assert.Equal(urls.Length, urls.Distinct(StringComparer.Ordinal).Count());
    }

    private static string[] Items(string group) =>
        OperationsNavigation.Groups.Single(chapter => chapter.Key == group).Items.Select(item => item.Section).ToArray();
}
