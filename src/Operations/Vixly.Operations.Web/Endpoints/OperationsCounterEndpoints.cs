using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Application.UseCases.Counter.AddOperationsCounterParcel;
using Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterInsurance;
using Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterParcelCount;
using Vixly.Operations.Application.UseCases.Counter.CreateOperationsCounterShipment;
using Vixly.Operations.Application.UseCases.Counter.EstimateOperationsCounterShipment;
using Vixly.Operations.Application.UseCases.Counter.FindOperationsCounterCustomers;
using Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterCustomer;
using Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterDeclaration;
using Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterShipment;
using Vixly.Operations.Application.UseCases.Counter.SaveOperationsCounterSignature;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Counter;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsCounterEndpoints.cs
//
// PURPOSE:
// The front-desk counter (Slice 5C): find the customer, see their shipments, create one, enter its parcels.
//
// RESPONSIBILITIES:
// - /staff/counter: find a customer by phone, email or customer code; one match opens them
// - /staff/counter/customers/{id}: the customer, all their shipments, a new shipment
// - /staff/counter/shipments/{id}: the shipment, its parcels against the count, the amount; enter a parcel; correct the count
//
// IMPORTANT NOTES:
// - Forms with antiforgery and the form guard (ADR-0019); the use cases check ops.counter.serve and the warehouse
// - A refused form is shown again with what the clerk typed
// =========================================================
public static class OperationsCounterEndpoints
{
    public static IEndpointRouteBuilder MapOperationsCounterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff/counter").RequireAuthorization().RequireFormToken();

        staff.MapGet("/", async (HttpContext http, string? find, FindOperationsCounterCustomersUseCase lookup, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            FindOperationsCounterCustomersResponse response = await lookup.ExecuteAsync(OperationsCurrentUser.Context(http),
                new FindOperationsCounterCustomersRequest { Query = find }, http.RequestAborted);

            // The one customer found: straight to them, as the clerk expects after typing a phone number.
            if (response.Succeeded && response.Customers.Count == 1)
            {
                return Results.Redirect("/staff/counter/customers/" + response.Customers[0].Id);
            }

            return OperationsStaffResults.Page<CounterPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                Find = find,
                Customers = response.Customers,
                Searched = !string.IsNullOrWhiteSpace(find),
                Error = errors.Describe(response.Error)
            });
        });

        staff.MapGet("/customers/{customerId:guid}", async (HttpContext http, Guid customerId, GetOperationsCounterCustomerUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
            await CustomerAsync(http, customerId, null, null, null, get, brand, errors));

        staff.MapPost("/customers/{customerId:guid}/shipments", async (HttpContext http, Guid customerId, [FromForm] Guid? origin, [FromForm] int? parcelCount, [FromForm] string? key,
            CreateOperationsCounterShipmentUseCase create, GetOperationsCounterCustomerUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            OperationsShipmentDraft draft = Draft(form);
            CreateOperationsCounterShipmentResponse response = await create.ExecuteAsync(OperationsCurrentUser.Context(http), new CreateOperationsCounterShipmentRequest
            {
                CustomerId = customerId,
                WarehouseId = origin ?? Guid.Empty,
                Draft = draft,
                ParcelCount = parcelCount ?? 0,
                RequestKey = key
            }, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/counter/shipments/" + response.ShipmentId + "?done=created")
                : await CustomerAsync(http, customerId, response.Error, draft, parcelCount, get, brand, errors);
        });

        staff.MapGet("/shipments/{shipmentId:guid}", async (HttpContext http, Guid shipmentId, string? done, string? screen, GetOperationsCounterShipmentUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await ShipmentAsync(http, shipmentId, OperationsScreenEndpoints.Refusal(screen), screen == "sent" ? text["ui.screens.sent"].Value
                : done is "created" or "added" or "booked" or "count" or "insurance" or "signed" ? text["ui.counter.done." + done].Value
                : done is "supply" or "supply_short" or "line" ? text["ui.counter.supplies.done." + done].Value : null, null, get, brand, errors));

        staff.MapPost("/shipments/{shipmentId:guid}/parcels", async (HttpContext http, Guid shipmentId, [FromForm] string? weight, [FromForm] string? length, [FromForm] string? width,
            [FromForm] string? height, [FromForm] string? contents, [FromForm] string? value, [FromForm] string? key, AddOperationsCounterParcelUseCase add,
            GetOperationsCounterShipmentUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            AddOperationsCounterParcelRequest request = new()
            {
                ShipmentId = shipmentId,
                Weight = Number(weight),
                Length = Number(length),
                Width = Number(width),
                Height = Number(height),
                Contents = contents,
                Value = Number(value),
                CommandKey = key
            };
            AddOperationsCounterParcelResponse response = await add.ExecuteAsync(OperationsCurrentUser.Context(http), request, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/counter/shipments/" + shipmentId + "?done=" + (response.Booked ? "booked" : "added"))
                : await ShipmentAsync(http, shipmentId, response.Error, null, request, get, brand, errors);
        });

        staff.MapPost("/shipments/{shipmentId:guid}/count", async (HttpContext http, Guid shipmentId, [FromForm] int? parcelCount, ChangeOperationsCounterParcelCountUseCase change,
            GetOperationsCounterShipmentUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ChangeOperationsCounterParcelCountResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ChangeOperationsCounterParcelCountRequest { ShipmentId = shipmentId, ParcelCount = parcelCount ?? 0 }, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/counter/shipments/" + shipmentId + "?done=count")
                : await ShipmentAsync(http, shipmentId, response.Error, null, null, get, brand, errors);
        });

        // Slice 5D: the shipment estimate while the clerk fills in a new shipment; the page's script sends the form here as it changes. Read only.
        staff.MapPost("/customers/{customerId:guid}/estimate", async (HttpContext http, Guid customerId, [FromForm] string? service,
            EstimateOperationsCounterShipmentUseCase estimate) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            EstimateOperationsCounterShipmentResponse response = await estimate.ExecuteAsync(OperationsCurrentUser.Context(http), new EstimateOperationsCounterShipmentRequest
            {
                CustomerId = customerId,
                OriginWarehouseId = Guid.TryParse(form["origin"], out Guid origin) ? origin : null,
                DestinationWarehouseId = Guid.TryParse(form["destination"], out Guid destination) ? destination : null,
                Delivery = form["delivery"].ToString(),
                ServiceCode = service,
                Consolidate = form["consolidate"] == "true",
                InsurancePlanId = Guid.TryParse(form["insurance"], out Guid product) ? product : null,
                ExpectedValue = Amount(form["expectedValue"])
            }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.Forbid();
            }

            http.Response.Headers.CacheControl = "no-store";
            return new Microsoft.AspNetCore.Http.HttpResults.RazorComponentResult<EstimatePanel>(new
            {
                Estimate = response.Estimate,
                WeightUnit = response.WeightUnit,
                DimensionUnit = response.DimensionUnit
            });
        }).RequireRateLimiting(OperationsRateLimits.Estimate);

        // Slice 5D (D-8): the page the customer signs, in their language.
        staff.MapGet("/shipments/{shipmentId:guid}/declaration", async (HttpContext http, Guid shipmentId, GetOperationsCounterDeclarationUseCase declaration) =>
        {
            GetOperationsCounterDeclarationResponse response = await declaration.ExecuteAsync(OperationsCurrentUser.Context(http),
                new GetOperationsCounterDeclarationRequest { ShipmentId = shipmentId }, http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Content, "application/pdf", response.FileName) : Results.NotFound();
        });

        // Slice 5D (D-8): the customer signs on screen; the page sends the drawing as a PNG data address.
        staff.MapPost("/shipments/{shipmentId:guid}/signature", async (HttpContext http, Guid shipmentId, [FromForm] string? signature, SaveOperationsCounterSignatureUseCase save,
            GetOperationsCounterShipmentUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            const string prefix = "data:image/png;base64,";
            byte[]? image = null;

            if (signature is { Length: > 0 and < 300_000 } && signature.StartsWith(prefix, StringComparison.Ordinal))
            {
                try
                {
                    image = Convert.FromBase64String(signature[prefix.Length..]);
                }
                catch (FormatException)
                {
                    image = null;
                }
            }

            SaveOperationsCounterSignatureResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http),
                new SaveOperationsCounterSignatureRequest { ShipmentId = shipmentId, Image = image }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/counter/shipments/" + shipmentId + "?done=signed")
                : await ShipmentAsync(http, shipmentId, response.Error, null, null, get, brand, errors);
        });

        // Slice 5D: the customer takes another insurance product, or none; they accept its terms and the clerk records it.
        staff.MapPost("/shipments/{shipmentId:guid}/insurance", async (HttpContext http, Guid shipmentId, [FromForm] string? insurance, [FromForm] string? insuranceAccepted,
            ChangeOperationsCounterInsuranceUseCase change, GetOperationsCounterShipmentUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ChangeOperationsCounterInsuranceResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http), new ChangeOperationsCounterInsuranceRequest
            {
                ShipmentId = shipmentId,
                InsurancePlanId = Guid.TryParse(insurance, out Guid product) ? product : null,
                Accepted = insuranceAccepted == "true"
            }, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/counter/shipments/" + shipmentId + "?done=insurance")
                : await ShipmentAsync(http, shipmentId, response.Error, null, null, get, brand, errors);
        });

        return endpoints;
    }

    // Slice 5D: an amount of money as people type it: "1234.5", "1234,5", "1,234.56", "1.234,56", "1 234"; a separator before exactly three digits
    // with no other separator groups thousands (money has two decimals). Null: not an amount.
    internal static decimal? Amount(string? typed)
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

    // The clerk types 2.5 or 2,5 (review M3 of Slice 5C): one decimal mark, at most 12 characters; anything else is refused by the rules (-1), never guessed.
    private static decimal? Number(string? typed)
    {
        string text = (typed ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            return null;
        }

        string dotted = text.Replace(',', '.');
        return text.Length <= 12 && dotted.All(character => char.IsAsciiDigit(character) || character == '.') && dotted.Count(character => character == '.') <= 1
            && decimal.TryParse(dotted, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out decimal number)
                ? number
                : -1m;
    }

    // The same fields as the customer's own shipment form (Slice 4C and 4D), the service required at the counter.
    private static OperationsShipmentDraft Draft(IFormCollection form) =>
        new(form["delivery"].ToString(),
            Guid.TryParse(form["branch"], out Guid branch) ? branch : null,
            form["line1"], form["line2"], form["city"], form["region"], form["postalCode"], form["country"],
            form["recipient"], form["recipientPhone"],
            form["consolidate"] == "true", form["refrigerated"] == "true", Guid.TryParse(form["insurance"], out Guid _) || form["insured"] == "true",
            Guid.TryParse(form["destination"], out Guid destination) ? destination : null,
            form["insuranceAccepted"] == "true", form["service"].ToString(), null, Guid.TryParse(form["insurance"], out Guid product) ? product : null, form["collect"] == "true");

    internal static async Task<IResult> CustomerAsync(HttpContext http, Guid customerId, Error? error, OperationsShipmentDraft? entered, int? enteredCount,
        GetOperationsCounterCustomerUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsCounterCustomerResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCounterCustomerRequest { CustomerId = customerId },
            http.RequestAborted);

        if (response.Customer is not { } customer)
        {
            return response.Error is { } refused && !refused.Equals(Vixly.Operations.Domain.Receiving.OperationsReceivingErrors.CustomerNotFound)
                ? OperationsStaffResults.Page<CounterPage>(http, new { Company = await OperationsStaffResults.CompanyAsync(http, brand), Error = errors.Describe(refused) })
                : Results.NotFound();
        }

        return OperationsStaffResults.Page<CounterCustomerPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Customer = customer,
            Standing = response.Standing,
            Shipments = response.Shipments,
            Origins = response.Origins,
            Destinations = response.Destinations,
            Services = response.Services,
            Insurance = response.Insurance,
            CurrencyCode = response.CurrencyCode,
            UtcNow = response.UtcNow,
            DeliveryModes = response.DeliveryModes,
            response.AllowCollect,
            Entered = entered,
            EnteredCount = enteredCount,
            Orders = response.Orders,
            Error = errors.Describe(error)
        });
    }

    internal static async Task<IResult> ShipmentAsync(HttpContext http, Guid shipmentId, Error? error, string? info, AddOperationsCounterParcelRequest? entered,
        GetOperationsCounterShipmentUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsCounterShipmentResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCounterShipmentRequest { ShipmentId = shipmentId },
            http.RequestAborted);

        if (response.Shipment is not { } shipment || response.Customer is not { } customer)
        {
            return response.Error is { } refused && !refused.Equals(Vixly.Operations.Domain.Shipments.OperationsShipmentErrors.NotFound)
                ? OperationsStaffResults.Page<CounterPage>(http, new { Company = await OperationsStaffResults.CompanyAsync(http, brand), Error = errors.Describe(refused) })
                : Results.NotFound();
        }

        return OperationsStaffResults.Page<CounterShipmentPage>(http, new
        {
            Supplies = response.Supplies,
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Customer = customer,
            Standing = response.Standing,
            Shipment = shipment,
            Insurance = response.Insurance,
            Premiums = response.Premiums,
            BaseTotal = response.BaseTotal,
            StaffMayChangeInsurance = response.StaffMayChangeInsurance,
            UtcNow = response.UtcNow,
            Panel = response.Panel,
            Invoice = response.Invoice,
            Parcels = response.Parcels,
            Estimate = response.Estimate,
            CurrencyCode = response.CurrencyCode,
            WeightUnit = response.WeightUnit,
            DimensionUnit = response.DimensionUnit,
            Entered = entered,
            Screens = await OperationsScreenEndpoints.ScreensAtAsync(http,
                http.RequestServices.GetRequiredService<Vixly.Operations.Application.UseCases.Screens.ListOperationsCustomerScreens.ListOperationsCustomerScreensUseCase>(),
                shipment.OriginWarehouseId),
            Error = errors.Describe(error),
            Info = info
        });
    }
}
