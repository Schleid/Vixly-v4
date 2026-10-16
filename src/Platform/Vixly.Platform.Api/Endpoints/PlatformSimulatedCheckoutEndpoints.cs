using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Infrastructure.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Api.Endpoints;

// =========================================================
// FILE: PlatformSimulatedCheckoutEndpoints.cs
//
// PURPOSE:
// The simulated provider's checkout page, so card payments can be tried in Development without Stripe.
//
// RESPONSIBILITIES:
// - Show the amount and three choices: pay with a test card, use a declined card, cancel
// - Complete the checkout and redirect like the real provider
//
// IMPORTANT NOTES:
// - Mapped only in Development and only when the simulated provider is in use
// - No real card is ever entered; plain HTML, no script
// =========================================================
public static class PlatformSimulatedCheckoutEndpoints
{
    public static IEndpointRouteBuilder MapPlatformSimulatedCheckout(this IEndpointRouteBuilder endpoints, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);

        if (!environment.IsDevelopment() || endpoints.ServiceProvider.GetRequiredService<IPlatformCardGateway>() is not SimulatedCardGateway)
        {
            return endpoints;
        }

        endpoints.MapGet("/dev/simulated-checkout/{id}", (string id, IPlatformCardGateway gateway) =>
        {
            if (gateway is not SimulatedCardGateway simulated || simulated.Checkout(id) is not { Status: "open" } checkout)
            {
                return Results.NotFound();
            }

            string what = checkout.Request.Purpose == PlatformCheckoutPurpose.SaveCard
                ? "Save a card"
                : "Pay " + (checkout.Request.AmountMinor / 100m).ToString("N2", CultureInfo.InvariantCulture) + " " + checkout.Request.CurrencyCode + " for " + checkout.Request.Description;
            string html = "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>Simulated checkout</title></head><body>"
                + "<h1>Simulated card provider (Development)</h1><p>" + WebUtility.HtmlEncode(what) + "</p>"
                + "<form method=\"post\"><button name=\"card\" value=\"4242\">Pay with test card 4242</button> "
                + "<button name=\"card\" value=\"declined\">Use a declined card</button> "
                + "<button name=\"card\" value=\"cancel\">Cancel</button></form></body></html>";
            return Results.Content(html, "text/html");
        });

        endpoints.MapPost("/dev/simulated-checkout/{id}", async (string id, HttpContext http, IPlatformCardGateway gateway, CancellationToken cancellationToken) =>
        {
            if (gateway is not SimulatedCardGateway simulated || simulated.Checkout(id) is not { Status: "open" } checkout)
            {
                return Results.NotFound();
            }

            IFormCollection form = await http.Request.ReadFormAsync(cancellationToken);
            string card = form["card"].ToString();

            if (card == "cancel")
            {
                return Results.Redirect(checkout.Request.CancelUrl);
            }

            try
            {
                return Results.Redirect(await simulated.CompleteAsync(id, card, cancellationToken));
            }
            catch (DomainException)
            {
                return Results.NotFound();
            }
        }).DisableAntiforgery();

        return endpoints;
    }
}
