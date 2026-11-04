using System.Net;
using Vixly.Contracts.Operations.Customers;
using Vixly.Contracts.Operations.Packages;

namespace Vixly.Customer.Web.Tests;

// =========================================================
// FILE: CustomerAccountHttpTests.cs
// PURPOSE:
// Exercises the optional first-login tour through the real portal.
// RESPONSIBILITIES:
// - Verify setup ordering, replay, durable dismissal and safe navigation.
// IMPORTANT NOTES:
// - Operations is a session-checking fixture; no business forms submit.
// =========================================================
public sealed partial class CustomerAccountHttpTests
{
    [Theory]
    [InlineData("en")][InlineData("fr")][InlineData("es")]
    public async Task TourRunsAfterSetupAndFinishesWithoutCreatingBusinessRecords(string language)
    {
        bool pending = true;
        CustomerAddressDto? address = null;
        _cookies["__Secure-vx4-customer"] = "session-1";
        _cookies["__Secure-vx4-customer-lang"] = "c=" + language + "|uic=" + language;
        _host.Operations.Customers = call => call switch
        {
            { Path: "me", Method: "GET", Session: "session-1" } => (HttpStatusCode.OK, Ana() with { HomeAddress = address, WelcomeTourPending = pending }),
            { Path: "me/shipments/choices", Method: "GET" } => (HttpStatusCode.OK, new CustomerShipmentChoicesDto(
                [new(Guid.NewGuid(), "Mailbox", "Demo city")], [new(Guid.NewGuid(), "Pickup", "Destination")], true, false, "Ana", "2025550100")),
            { Path: "me/packages", Method: "GET" } => (HttpStatusCode.OK, new CustomerPackagesDto([], [], "lb", "in")),
            { Path: "me/tour/dismiss", Method: "POST", Session: "session-1" } => (HttpStatusCode.NoContent, pending = false),
            _ => (HttpStatusCode.Unauthorized, Error("ops.customers.session_required"))
        };
        (HttpResponseMessage setup, _) = await GetAsync("/customer/");
        Assert.Equal("/customer/address", setup.Headers.Location?.OriginalString);
        address = new("20 Example Road", null, "Les Cayes", null, null, "HT");
        (_, string first) = await GetAsync("/customer/");
        Assert.Contains("data-customer-tour", first);
        Assert.Contains("/customer/packages/new?tour=2", first);
        (_, string second) = await GetAsync("/customer/packages/new?tour=2");
        Assert.Contains("data-customer-tour", second);
        Assert.Contains("/customer/packages/shipments/new?tour=3", second);
        (_, string third) = await GetAsync("/customer/packages/shipments/new?tour=3");
        Assert.Contains("data-customer-tour", third);
        Assert.DoesNotContain("ui.tour.", first + second + third);
        Assert.All(_host.Operations.Requests, call => Assert.Equal("GET", call.Method));
        HttpResponseMessage finished = await PostFormAsync("/customer/packages/shipments/new?tour=3", "/customer/tour/dismiss", new());
        Assert.Equal("/customer/", finished.Headers.Location?.OriginalString);
        Assert.False(pending);
        (_, string later) = await GetAsync("/customer/");
        Assert.DoesNotContain("data-customer-tour", later);
        (_, string help) = await GetAsync("/customer/help");
        Assert.Contains("href=\"/customer/tour\"", help);
        (HttpResponseMessage replay, _) = await GetAsync("/customer/tour");
        Assert.Equal("/customer/?tour=1", replay.Headers.Location?.OriginalString);
        (_, string replayed) = await GetAsync("/customer/?tour=1");
        Assert.Contains("data-customer-tour", replayed);
        Assert.Single(_host.Operations.Requests, call => call.Method == "POST");
    }

    [Fact]
    public async Task TourDismissalRequiresAntiforgeryAndKeepsAVisibleRetryOnFailure()
    {
        _cookies["__Secure-vx4-customer"] = "session-1";
        _host.Operations.Customers = call => call.Path == "me"
            ? (HttpStatusCode.OK, Ana() with { HomeAddress = new("20 Example Road", null, "Les Cayes", null, null, "HT"), WelcomeTourPending = true })
            : (HttpStatusCode.ServiceUnavailable, null);
        using HttpResponseMessage rejected = await SendAsync(HttpMethod.Post, "/customer/tour/dismiss", new());
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.DoesNotContain(_host.Operations.Requests, call => call.Path == "me/tour/dismiss");
        using HttpResponseMessage failed = await PostFormAsync("/customer/", "/customer/tour/dismiss", new());
        Assert.Equal("/customer/?tour=1&tourError=true", failed.Headers.Location?.OriginalString);
        (_, string retry) = await GetAsync(failed.Headers.Location!.OriginalString);
        Assert.Contains("data-customer-tour", retry);
        Assert.Contains("could not save your tour preference", retry);
    }

    [Fact]
    public async Task TourDoesNotTrustAStaffCookieOrAnExpiredCustomerSession()
    {
        _cookies["__Host-vx4-ops"] = "staff";
        (HttpResponseMessage signedOut, _) = await GetAsync("/customer/tour");
        Assert.Equal("/customer/sign-in", signedOut.Headers.Location?.OriginalString);
        _cookies["__Secure-vx4-customer"] = "expired";
        _host.Operations.Customers = _ => (HttpStatusCode.Unauthorized, Error("ops.customers.session_required"));
        (HttpResponseMessage expired, _) = await GetAsync("/customer/tour");
        Assert.Equal("/customer/sign-in", expired.Headers.Location?.OriginalString);
    }
}
