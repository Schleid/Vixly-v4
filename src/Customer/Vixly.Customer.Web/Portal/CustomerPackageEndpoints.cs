using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Localization;
using Vixly.Contracts.Operations.Customers;
using Vixly.Contracts.Operations.Packages;
using Vixly.Customer.Resources;
using Vixly.Customer.Web.Components.Portal;
using Vixly.Customer.Web.Operations;
using Vixly.Customer.Web.Tenancy;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerPackageEndpoints.cs
//
// PURPOSE:
// The customer's package pages (Slice 3B, design section 7).
//
// RESPONSIBILITIES:
// - /customer/packages: pre-alerts and packages
// - /customer/packages/new: announce a package on its own (Slice 4D); withdraw a pre-alert
// - /customer/packages/move: put a package into a shipment, move it, take it out (Slice 4D)
// - /customer/packages/{id}: one package with its history and photos; the photo proxy
//
// IMPORTANT NOTES:
// - Signed-in pages; Operations checks the session and the ownership on every call (S-3)
// - Photos come through this site from Operations, never from a guessable address
// - Nothing is cached (no-store); forms carry antiforgery (ADR-0019)
// =========================================================
public static class CustomerPackageEndpoints
{
    public static IEndpointRouteBuilder MapCustomerPackageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder portal = endpoints.MapGroup("/packages");
        portal.WithMetadata(new RequireAntiforgeryTokenAttribute());
        portal.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return context.HttpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false } ? Results.BadRequest() : await next(context);
        });

        portal.MapGet("/", async (HttpContext http, string? done, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
            await ListAsync(http, api, text, null, done is "added" or "withdrawn" or "matched" or "moved" ? text["ui.packages.done." + done].Value
                : done == "cancelled" ? text["ui.shipments.done.cancelled"].Value : null));

        // Slice 4D: pre-alerts come first; a shipment may be chosen now or later (owner, 2026-10-19).
        portal.MapGet("/new", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) => await PreAlertFormAsync(http, api, text, null, null));

        portal.MapPost("/new", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            CustomerPreAlertBody package = CustomerShipmentEndpoints.Package(form, Guid.TryParse(form["shipment"], out Guid shipment) ? shipment : null);
            CustomerCall<CustomerPreAlertCreatedDto> call = await api.CreatePreAlertAsync(signedIn.Tenant, signedIn.Session, package, http, http.RequestAborted);

            if (call.Value is not { } created)
            {
                return await PreAlertFormAsync(http, api, text, package, CustomerPortalEndpoints.Describe(text, call));
            }

            string done = created.ParcelId is null ? "added" : "matched";
            return Results.Redirect(package.ShipmentId is { } chosen ? "/customer/packages/shipments/" + chosen + "?done=" + done : "/customer/packages?done=" + done);
        });

        portal.MapPost("/move", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            CustomerPackageMoveBody body = new(Guid.TryParse(form["preAlert"], out Guid preAlert) ? preAlert : null, Guid.TryParse(form["parcel"], out Guid parcel) ? parcel : null,
                Guid.TryParse(form["shipment"], out Guid shipment) ? shipment : null);
            CustomerCall<object> call = await api.MovePackageAsync(signedIn.Tenant, signedIn.Session, body, http, http.RequestAborted);

            // Back only to the packages page or one of the customer's shipment pages, never to an address from the form.
            Guid? back = form["back"].ToString() is { } typed && typed.StartsWith("/customer/packages/shipments/", StringComparison.Ordinal)
                && Guid.TryParse(typed["/customer/packages/shipments/".Length..], out Guid backTo) ? backTo : null;

            if (call.Succeeded)
            {
                return Results.Redirect(back is { } page ? "/customer/packages/shipments/" + page + "?done=moved" : "/customer/packages?done=moved");
            }

            string error = CustomerPortalEndpoints.Describe(text, call);
            return back is { } shown ? await CustomerShipmentEndpoints.ShipmentAsync(http, api, text, shown, null, error, null) : await ListAsync(http, api, text, error, null);
        });

        portal.MapPost("/pre-alerts/{preAlertId:guid}/withdraw", async (HttpContext http, Guid preAlertId, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            CustomerCall<object> call = await api.CancelPreAlertAsync(signedIn.Tenant, signedIn.Session, preAlertId, http, http.RequestAborted);
            return call.Succeeded ? Results.Redirect("/customer/packages?done=withdrawn") : await ListAsync(http, api, text, CustomerPortalEndpoints.Describe(text, call), null);
        });

        portal.MapGet("/{parcelId:guid}", async (HttpContext http, Guid parcelId, string? done, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
            await PackageAsync(http, parcelId, api, null, done is "matched" or "answered" ? text["ui.packages.done." + done].Value : null));

        // Slice 3E: answering a hold; a document of up to 10 MB (PDF, PNG or JPEG), checked again by Operations.
        portal.MapPost("/{parcelId:guid}/answers", async (HttpContext http, Guid parcelId, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            IFormCollection form;

            // A file above the form limit fails while the form is read: the customer sees the size rule, not an error page.
            try
            {
                form = await http.Request.ReadFormAsync(http.RequestAborted);
            }
            catch (Exception exception) when (exception is InvalidDataException or BadHttpRequestException)
            {
                return await PackageAsync(http, parcelId, api, text["ops.holds.document_invalid"].Value, null);
            }

            byte[]? document = null;
            string? fileName = null;

            if (form.Files.GetFile("document") is { Length: > 0 } file)
            {
                if (file.Length > MaxDocumentBytes)
                {
                    return await PackageAsync(http, parcelId, api, text["ops.holds.document_invalid"].Value, null);
                }

                using MemoryStream buffer = new((int)file.Length);
                await file.CopyToAsync(buffer, http.RequestAborted);
                (document, fileName) = (buffer.ToArray(), file.FileName);
            }

            CustomerCall<object> call = await api.AnswerHoldAsync(signedIn.Tenant, signedIn.Session, parcelId, new CustomerHoldAnswerBody(
                form["reason"].ToString(),
                form["contents"].ToString(),
                decimal.TryParse(form["value"].ToString(), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out decimal value) ? value : null,
                form["choice"].ToString(),
                form["note"].ToString(),
                fileName,
                document), http, http.RequestAborted);

            return call.Succeeded
                ? Results.Redirect("/customer/packages/" + parcelId + "?done=answered")
                : await PackageAsync(http, parcelId, api, CustomerPortalEndpoints.Describe(text, call), null);
        }).WithFormOptions(multipartBodyLengthLimit: MaxDocumentBytes + (1024 * 1024));

        portal.MapGet("/{parcelId:guid}/photos/{photoId:guid}", async (HttpContext http, Guid parcelId, Guid photoId, CustomerOperationsApi api) =>
        {
            if (Signed(http) is not { } signedIn
                || await api.GetPhotoAsync(signedIn.Tenant, signedIn.Session, parcelId, photoId, http, http.RequestAborted) is not { } photo)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; img-src 'self'; sandbox";
            return Results.Bytes(photo.Content, photo.ContentType);
        });

        return endpoints;
    }

    private const int MaxDocumentBytes = 10 * 1024 * 1024;

    private static async Task<IResult> PackageAsync(HttpContext http, Guid parcelId, CustomerOperationsApi api, string? error, string? info)
    {
        if (Signed(http) is not { } signedIn)
        {
            return Results.Redirect("/customer/sign-in");
        }

        CustomerCall<CustomerParcelDetailDto> call = await api.GetPackageAsync(signedIn.Tenant, signedIn.Session, parcelId, http, http.RequestAborted);

        if (call.ErrorCode == "ops.customers.session_required")
        {
            CustomerSessionCookie.Delete(http.Response);
            return Results.Redirect("/customer/sign-in");
        }

        return call.Value is { } detail
            ? Page<PackagePage>(new { Brand = await api.GetBrandAsync(signedIn.Tenant, http.RequestAborted), Detail = detail, Error = error, Info = info })
            : Results.NotFound();
    }

    private static (Guid Tenant, string Session)? Signed(HttpContext http) =>
        CustomerTenantMiddleware.Host(http)?.TenantId is { } tenant && CustomerSessionCookie.Read(http.Request) is { } session ? (tenant, session) : null;

    private static async Task<IResult> PreAlertFormAsync(HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text, CustomerPreAlertBody? entered, string? error)
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

        CustomerPackagesDto? packages = (await api.ListPackagesAsync(signedIn.Tenant, signedIn.Session, http, http.RequestAborted)).Value;

        return Page<PreAlertFormPage>(new
        {
            Brand = await api.GetBrandAsync(signedIn.Tenant, http.RequestAborted),
            Choices = choices.Value,
            Shipments = CustomerPackagePlaces.Open(packages, null),
            Entered = entered,
            Error = error ?? (choices.Value is null ? CustomerPortalEndpoints.Describe(text, choices) : null)
        });
    }

    private static async Task<IResult> ListAsync(HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text, string? error, string? info)
    {
        if (Signed(http) is not { } signedIn)
        {
            return Results.Redirect("/customer/sign-in");
        }

        CustomerCall<CustomerPackagesDto> call = await api.ListPackagesAsync(signedIn.Tenant, signedIn.Session, http, http.RequestAborted);

        if (call.ErrorCode == "ops.customers.session_required")
        {
            CustomerSessionCookie.Delete(http.Response);
            return Results.Redirect("/customer/sign-in");
        }

        return Page<PackagesPage>(new
        {
            Brand = await api.GetBrandAsync(signedIn.Tenant, http.RequestAborted),
            Packages = call.Value,
            Error = error ?? (call.Value is null ? CustomerPortalEndpoints.Describe(text, call) : null),
            Info = info
        });
    }

    private static IResult Page<TComponent>(object parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent => new RazorComponentResult<TComponent>(parameters);
}
