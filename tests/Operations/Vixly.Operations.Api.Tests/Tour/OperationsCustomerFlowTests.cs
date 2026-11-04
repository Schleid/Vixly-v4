using System.Net;
using Microsoft.EntityFrameworkCore;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Api.Tests;

// =========================================================
// FILE: OperationsCustomerFlowTests.cs
// PURPOSE:
// Proves tour preferences persist through the real API and database.
// RESPONSIBILITIES:
// - Keep another company's customer unchanged and reject stolen sessions.
// IMPORTANT NOTES:
// - Requires VIXLY_TEST_SQL_SERVER; never touches a deployed database.
// =========================================================
public sealed partial class OperationsCustomerFlowTests
{
    [SqlServerFact]
    public async Task TourDismissalPersistsAcrossSignInsAndCannotChangeAnotherTenant()
    {
        const string email = "tour.customer@example.com";
        Browser north = await ConfirmedAsync(NorthSlug, email);
        Browser south = await ConfirmedAsync(SouthSlug, email);
        Assert.Contains("data-customer-tour", await north.GetStringAsync("/customer/"));
        Assert.Contains("data-customer-tour", await south.GetStringAsync("/customer/"));
        HttpResponseMessage dismissed = await north.PostAsync("/customer/", "/customer/tour/dismiss", new());
        Assert.Equal("/customer/", dismissed.Headers.Location?.OriginalString);
        DateTimeOffset? saved = null;
        await WithDatabaseAsync(_north, async context =>
        {
            var customer = await context.Customers.SingleAsync(item => item.Email == EmailAddress.Create(email));
            Assert.NotNull(customer.WelcomeTourDismissedUtc);
            saved = customer.WelcomeTourDismissedUtc;
        });
        Browser later = OpenBrowser(NorthSlug);
        await later.PostAsync("/customer/sign-in", "/customer/sign-in", new() { ["email"] = email, ["password"] = Password });
        Assert.DoesNotContain("data-customer-tour", await later.GetStringAsync("/customer/"));
        Assert.Contains("data-customer-tour", await later.GetStringAsync("/customer/?tour=1"));
        await later.PostAsync("/customer/?tour=1", "/customer/tour/dismiss", new());
        await WithDatabaseAsync(_north, async context =>
            Assert.Equal(saved, (await context.Customers.SingleAsync(item => item.Email == EmailAddress.Create(email))).WelcomeTourDismissedUtc));
        Browser thief = OpenBrowser(SouthSlug);
        thief.Cookies["__Secure-vx4-customer"] = north.Cookies["__Secure-vx4-customer"];
        HttpResponseMessage refused = await thief.PostAsync("/customer/sign-in", "/customer/tour/dismiss", new());
        Assert.Equal("/customer/sign-in", refused.Headers.Location?.OriginalString);
        await WithDatabaseAsync(_south, async context =>
            Assert.Null((await context.Customers.SingleAsync(item => item.Email == EmailAddress.Create(email))).WelcomeTourDismissedUtc));
    }
}
