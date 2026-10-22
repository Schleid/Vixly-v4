using System.Globalization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Localization;
using Vixly.Contracts.Operations.Packages;
using Vixly.Customer.Resources;
using Vixly.Customer.Web.Components.Portal;
using Vixly.Customer.Web.Operations;
using Vixly.Customer.Web.Tenancy;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerShipmentEndpoints.cs
//
// PURPOSE:
// The customer's shipments: where they go, who receives them, the options and their packages (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - /customer/packages/shipments/new: create a shipment, with packages announced before or none (Slice 4D)
// - /customer/packages/shipments/{id}: the shipment, its packages; add or put in a package, change the instructions, cancel
//
// IMPORTANT NOTES:
// - Signed-in pages; Operations checks the session and the ownership on every call (S-3)
// - Under /packages, so the menu and the antiforgery filter of the package pages apply; nothing is cached
// =========================================================
public static class CustomerShipmentEndpoints
{
    public static IEndpointRouteBuilder MapCustomerShipmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder portal = endpoints.MapGroup("/packages/shipments");
        portal.WithMetadata(new RequireAntiforgeryTokenAttribute());
        portal.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return context.HttpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false } ? Results.BadRequest() : await next(context);
        });

        portal.MapGet("/new", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
            await FormAsync(http, api, text, null, null, Array.Empty<Guid>(), null));

        portal.MapPost("/new", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            CustomerShipmentInstructionsBody instructions = Instructions(form);
            List<Guid> preAlerts = Ids(form["preAlerts"]);
            List<Guid> parcels = Ids(form["parcels"]);

            // A new package with it only when one was typed.
            CustomerPreAlertBody? package = string.IsNullOrWhiteSpace(form["tracking"]) ? null : Package(form, null);
            CustomerCall<CustomerShipmentCreatedDto> call = await api.CreateShipmentAsync(signedIn.Tenant, signedIn.Session,
                new CustomerShipmentBody(Guid.TryParse(form["origin"], out Guid origin) ? origin : Guid.Empty, instructions, package, preAlerts, parcels, form["key"].ToString()), http, http.RequestAborted);

            return call.Value is { } created
                ? Results.Redirect("/customer/packages/shipments/" + created.ShipmentId + "?done=" + (created.ParcelId is null ? "created" : "matched"))
                : await FormAsync(http, api, text, null, instructions, preAlerts.Concat(parcels).ToList(), CustomerPortalEndpoints.Describe(text, call), package);
        });

        // Slice 5D: the shipment estimate beside the form; the page's script sends the form here as it changes. Read only.
        portal.MapPost("/estimate", async (HttpContext http, CustomerOperationsApi api) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Unauthorized();
            }

            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            decimal? expected = Amount(form["expectedValue"]);
            CustomerCall<Vixly.Contracts.Operations.Rates.CustomerShipmentEstimateDto> call = await api.EstimateShipmentAsync(signedIn.Tenant, signedIn.Session,
                new Vixly.Contracts.Operations.Rates.CustomerShipmentEstimateBody(Instructions(form), Guid.TryParse(form["origin"], out Guid origin) ? origin : null, expected,
                    Guid.TryParse(form["estimateShipment"], out Guid shipment) ? shipment : null), http, http.RequestAborted);
            return new Microsoft.AspNetCore.Http.HttpResults.RazorComponentResult<Vixly.Customer.Web.Components.Portal.ShipmentEstimatePanel>(new { Estimate = call.Value });
        }).RequireRateLimiting(CustomerRateLimits.Estimate);

        portal.MapGet("/{shipmentId:guid}", async (HttpContext http, Guid shipmentId, string? done, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
            await ShipmentAsync(http, api, text, shipmentId, null, null, done is "created" or "matched" or "added" or "changed" or "moved" or "collector" ? text["ui.shipments.done." + done].Value : null));

        // Slice 7 step 2 (R-6): the person the customer names to collect it, or none.
        portal.MapPost("/{shipmentId:guid}/collector", async (HttpContext http, Guid shipmentId, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            CustomerCall<object> call = await api.ChangeCollectorAsync(signedIn.Tenant, signedIn.Session, shipmentId,
                new CustomerCollectorBody(form["collectorName"].ToString(), form["collectorPhone"].ToString()), http, http.RequestAborted);
            return call.Succeeded
                ? Results.Redirect("/customer/packages/shipments/" + shipmentId + "?done=collector")
                : await ShipmentAsync(http, api, text, shipmentId, null, CustomerPortalEndpoints.Describe(text, call), null);
        });

        portal.MapPost("/{shipmentId:guid}/packages", async (HttpContext http, Guid shipmentId, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            CustomerPreAlertBody package = Package(await http.Request.ReadFormAsync(http.RequestAborted), shipmentId);
            CustomerCall<CustomerPreAlertCreatedDto> call = await api.CreatePreAlertAsync(signedIn.Tenant, signedIn.Session, package, http, http.RequestAborted);

            return call.Value is { } created
                ? Results.Redirect("/customer/packages/shipments/" + shipmentId + "?done=" + (created.ParcelId is null ? "added" : "matched"))
                : await ShipmentAsync(http, api, text, shipmentId, package, CustomerPortalEndpoints.Describe(text, call), null);
        });

        // Slice 5A: the packing list and the pro forma commercial invoice; Slice 5B: the insurance certificate; through this site from Operations.
        portal.MapGet("/{shipmentId:guid}/certificate", async (HttpContext http, Guid shipmentId, CustomerOperationsApi api) =>
        {
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session
                || await api.GetShipmentDocumentAsync(tenant, session, shipmentId, "certificate", http, http.RequestAborted) is not { } document)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            return Results.File(document.Content, "application/pdf", document.FileName);
        });

        portal.MapGet("/{shipmentId:guid}/documents/{kind}", async (HttpContext http, Guid shipmentId, string kind, CustomerOperationsApi api) =>
        {
            if (kind is not ("packing-list" or "commercial-invoice")
                || CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session
                || await api.GetShipmentDocumentAsync(tenant, session, shipmentId, kind, http, http.RequestAborted) is not { } document)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            return Results.File(document.Content, "application/pdf", document.FileName);
        });

        portal.MapGet("/{shipmentId:guid}/edit", async (HttpContext http, Guid shipmentId, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
            await FormAsync(http, api, text, shipmentId, null, Array.Empty<Guid>(), null));

        portal.MapPost("/{shipmentId:guid}/edit", async (HttpContext http, Guid shipmentId, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            CustomerShipmentInstructionsBody instructions = Instructions(await http.Request.ReadFormAsync(http.RequestAborted));
            CustomerCall<object> call = await api.ChangeShipmentAsync(signedIn.Tenant, signedIn.Session, shipmentId, instructions, http, http.RequestAborted);

            return call.Succeeded
                ? Results.Redirect("/customer/packages/shipments/" + shipmentId + "?done=changed")
                : await FormAsync(http, api, text, shipmentId, instructions, Array.Empty<Guid>(), CustomerPortalEndpoints.Describe(text, call));
        });

        portal.MapPost("/{shipmentId:guid}/cancel", async (HttpContext http, Guid shipmentId, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            CustomerCall<object> call = await api.CancelShipmentAsync(signedIn.Tenant, signedIn.Session, shipmentId, http, http.RequestAborted);
            return call.Succeeded
                ? Results.Redirect("/customer/packages?done=cancelled")
                : await ShipmentAsync(http, api, text, shipmentId, null, CustomerPortalEndpoints.Describe(text, call), null);
        });

        return endpoints;
    }

    private static (Guid Tenant, string Session)? Signed(HttpContext http) =>
        CustomerTenantMiddleware.Host(http)?.TenantId is { } tenant && CustomerSessionCookie.Read(http.Request) is { } session ? (tenant, session) : null;

    private static CustomerShipmentInstructionsBody Instructions(IFormCollection form) =>
        new(form["delivery"].ToString(),
            Guid.TryParse(form["branch"], out Guid branch) ? branch : null,
            new CustomerAddressDto(form["line1"], form["line2"], form["city"], form["region"], form["postalCode"], form["country"]),
            form["recipient"], form["recipientPhone"],
            form["consolidate"] == "true", form["refrigerated"] == "true", form["insured"] == "true" || Guid.TryParse(form["insurance"], out Guid _),
            Guid.TryParse(form["destination"], out Guid destination) ? destination : null, form["insuranceAccepted"] == "true", form["service"].ToString(),
            int.TryParse(form["parcelCount"], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int count) ? count : null,
            Guid.TryParse(form["insurance"], out Guid product) ? product : null, form["collect"] == "true");

    // Slice 4D: the ticked packages; anything unreadable is left out.
    private static List<Guid> Ids(Microsoft.Extensions.Primitives.StringValues values) =>
        values.Select(value => Guid.TryParse(value, out Guid id) ? id : Guid.Empty).Where(id => id != Guid.Empty).Distinct().Take(50).ToList();

    // The value field sends a dot; a group separator such as 1,200 is refused, never read as 1.2 (review M3).
    internal static CustomerPreAlertBody Package(IFormCollection form, Guid? shipmentId)
    {
        string typed = form["value"].ToString().Trim();

        return new CustomerPreAlertBody(form["carrier"], form["tracking"], form["store"], form["contents"],
            typed.Length is > 0 and <= 20 && typed.All(character => char.IsAsciiDigit(character) || character == '.')
            && decimal.TryParse(typed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal declared) ? declared : -1m,
            DateOnly.TryParseExact(form["expected"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date) ? date : null,
            shipmentId,
            Guid.TryParse(form["warehouse"], out Guid warehouse) ? warehouse : null);
    }

    // A new shipment (no id) or the instructions of an open one, with what was typed after a refusal.
    private static async Task<IResult> FormAsync(HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text, Guid? shipmentId,
        CustomerShipmentInstructionsBody? entered, IReadOnlyCollection<Guid> chosen, string? error, CustomerPreAlertBody? package = null)
    {
        if (Signed(http) is not { } signedIn)
        {
            return Results.Redirect("/customer/sign-in");
        }

        CustomerCall<CustomerShipmentChoicesDto> choices = await api.GetShipmentChoicesAsync(signedIn.Tenant, signedIn.Session, http, http.RequestAborted);

        if (choices.ErrorCode == "ops.customers.session_required")
        {
            CustomerSessionCookie.Delete(http.Response);
            return Results.Redirect("/customer/sign-in");
        }

        CustomerShipmentDto? current = null;

        if (shipmentId is { } id)
        {
            CustomerCall<CustomerShipmentDetailDto> call = await api.GetShipmentAsync(signedIn.Tenant, signedIn.Session, id, http, http.RequestAborted);

            if (call.Value is not { Shipment: { IsOpen: true } open })
            {
                return Results.NotFound();
            }

            current = open;
        }

        // Slice 4D: the packages in no shipment yet, to tick for a new shipment.
        CustomerPackagesDto? packages = shipmentId is null ? (await api.ListPackagesAsync(signedIn.Tenant, signedIn.Session, http, http.RequestAborted)).Value : null;

        return Page<ShipmentFormPage>(new
        {
            Brand = await api.GetBrandAsync(signedIn.Tenant, http.RequestAborted),
            Choices = choices.Value,
            ShipmentId = shipmentId,
            Entered = entered ?? (current is null ? null : new CustomerShipmentInstructionsBody(current.Delivery, current.BranchId, current.Address, current.RecipientName, current.RecipientPhone,
                current.Consolidate, current.Refrigerated, current.Insured, current.DestinationId, false, current.Service, current.ParcelCount, current.InsurancePlanId,
                current.Collect)),
            UnplacedPreAlerts = CustomerPackagePlaces.UnplacedPreAlerts(packages, null),
            UnplacedParcels = CustomerPackagePlaces.UnplacedParcels(packages, null),
            Chosen = chosen,
            Package = package,
            Error = error ?? (choices.Value is null ? CustomerPortalEndpoints.Describe(text, choices) : null)
        });
    }

    internal static async Task<IResult> ShipmentAsync(HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text, Guid shipmentId, CustomerPreAlertBody? package,
        string? error, string? info)
    {
        if (Signed(http) is not { } signedIn)
        {
            return Results.Redirect("/customer/sign-in");
        }

        CustomerCall<CustomerShipmentDetailDto> call = await api.GetShipmentAsync(signedIn.Tenant, signedIn.Session, shipmentId, http, http.RequestAborted);

        if (call.ErrorCode == "ops.customers.session_required")
        {
            CustomerSessionCookie.Delete(http.Response);
            return Results.Redirect("/customer/sign-in");
        }

        if (call.Value is not { } detail)
        {
            return Results.NotFound();
        }

        // Slice 4D: the customer's packages from this shipment's warehouse that are in no shipment yet.
        CustomerPackagesDto? packages = detail.Shipment.IsOpen ? (await api.ListPackagesAsync(signedIn.Tenant, signedIn.Session, http, http.RequestAborted)).Value : null;

        return Page<ShipmentPage>(new
        {
            Brand = await api.GetBrandAsync(signedIn.Tenant, http.RequestAborted), Detail = detail, Package = package, Error = error, Info = info,
            UnplacedPreAlerts = CustomerPackagePlaces.UnplacedPreAlerts(packages, detail.Shipment.OriginId),
            UnplacedParcels = CustomerPackagePlaces.UnplacedParcels(packages, detail.Shipment.OriginId),
            Choices = CustomerPackagePlaces.Open(packages, detail.Shipment.OriginId)
        });
    }

    private static IResult Page<TComponent>(object parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent => new RazorComponentResult<TComponent>(parameters);

    // Slice 5D: an amount of money as people type it: "1234.5", "1234,5", "1,234.56", "1.234,56", "1 234"; a separator before exactly three digits
    // with no other separator groups thousands (money has two decimals). Null: not an amount.
    private static decimal? Amount(string? typed)
    {
        string text = new((typed ?? string.Empty).Where(character => !char.IsWhiteSpace(character) && character != '\u00A0').ToArray());
        int comma = text.LastIndexOf(','), dot = text.LastIndexOf('.');

        if (text.Length is 0 or > 16 || !text.All(character => char.IsAsciiDigit(character) || character is ',' or '.'))
        {
            return null;
        }

        char? decimals = comma >= 0 && dot >= 0 ? (comma > dot ? ',' : '.')
            : comma < 0 && dot < 0 ? null
            : (comma >= 0 ? ',' : '.') is var only && text.Count(character => character == only) == 1 && text.Length - text.LastIndexOf(only) - 1 != 3 ? only : null;
        string plain = new(text.Where(character => char.IsAsciiDigit(character) || character == decimals).Select(character => character == decimals ? '.' : character).ToArray());

        return decimal.TryParse(plain, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out decimal value)
            && value <= 1_000_000m ? value : null;
    }
}
