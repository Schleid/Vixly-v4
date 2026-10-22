using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Dispatch.GetOperationsCarriers;
using Vixly.Operations.Application.UseCases.Dispatch.GetOperationsCarrier;
using Vixly.Operations.Application.UseCases.Dispatch.SaveOperationsCarrier;
using Vixly.Operations.Application.UseCases.Dispatch.AddOperationsCarrierRate;
using Vixly.Operations.Application.UseCases.Dispatch.RemoveOperationsCarrierRate;
using Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatches;
using Vixly.Operations.Application.UseCases.Dispatch.CreateOperationsDispatch;
using Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatch;
using Vixly.Operations.Application.UseCases.Dispatch.AddBundleToOperationsDispatch;
using Vixly.Operations.Application.UseCases.Dispatch.RemoveBundleFromOperationsDispatch;
using Vixly.Operations.Application.UseCases.Dispatch.AddOperationsDispatchLeg;
using Vixly.Operations.Application.UseCases.Dispatch.RemoveOperationsDispatchLeg;
using Vixly.Operations.Application.UseCases.Dispatch.CloseOperationsDispatch;
using Vixly.Operations.Application.UseCases.Dispatch.DepartOperationsDispatchLeg;
using Vixly.Operations.Application.UseCases.Dispatch.ArriveOperationsDispatchLeg;
using Vixly.Operations.Application.UseCases.Dispatch.CancelOperationsDispatch;
using Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatchManifest;
using Vixly.Operations.Application.UseCases.Dispatch.GetOperationsBundleManifest;
using Vixly.Operations.Application.UseCases.Dispatch.RecostOperationsDispatch;
using Vixly.Operations.Application.UseCases.Dispatch.CorrectOperationsDispatchCost;
using Vixly.Operations.Application.UseCases.Dispatch.SaveOperationsDispatchInstructions;
using Vixly.Operations.Application.UseCases.Dispatch.GetOperationsHouseWaybills;
using Vixly.Operations.Application.UseCases.Dispatch.GetOperationsShipperLetter;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Dispatch;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsDispatchEndpoints.cs
//
// PURPOSE:
// Dispatches and carriers (Slice 6B): the dispatch desk, a dispatch and its steps, the manifests, the company's carriers and their rates.
//
// RESPONSIBILITIES:
// - /staff/dispatches and its forms; /staff/dispatches/{id}/manifest; /staff/bundles/{id}/manifest; /staff/carriers
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); every step comes back to the dispatch
// - Times are typed where the company works; the use cases check every permission
// =========================================================
public static class OperationsDispatchEndpoints
{
    private static readonly string[] Done = { "created", "closed", "departed", "arrived", "cancelled", "bundle", "removed", "leg", "costed", "cost", "instructions" };

    public static IEndpointRouteBuilder MapOperationsDispatchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/dispatches", async (HttpContext http, string? view, GetOperationsDispatchesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await DeskAsync(http, view, null, get, brand, errors));

        staff.MapPost("/dispatches", async (HttpContext http, [FromForm] Guid? warehouse, [FromForm] string? destination, [FromForm] string? service, [FromForm] string? cutoff,
            [FromForm] string? notes, [FromForm] string? key, CreateOperationsDispatchUseCase create, GetOperationsDispatchesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            CreateOperationsDispatchResponse response = await create.ExecuteAsync(OperationsCurrentUser.Context(http), new CreateOperationsDispatchRequest
            {
                WarehouseId = warehouse ?? Guid.Empty,
                DestinationWarehouseId = destination is { Length: 34 } && destination.StartsWith("w:", StringComparison.Ordinal) && Guid.TryParseExact(destination[2..], "N", out Guid to) ? to : null,
                CountryCode = destination is { Length: 4 } && destination.StartsWith("c:", StringComparison.Ordinal) ? destination[2..] : null,
                ServiceCode = service,
                Cutoff = Local(cutoff),
                Notes = notes,
                RequestKey = Guid.TryParseExact(key, "N", out Guid formKey) ? formKey : null
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/dispatches/" + response.DispatchId + "?done=created") : await DeskAsync(http, null, response.Error, get, brand, errors);
        });

        staff.MapGet("/dispatches/{dispatchId:guid}", async (HttpContext http, Guid dispatchId, string? done, GetOperationsDispatchUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
            await DispatchAsync(http, dispatchId, null, done is not null && Done.Contains(done, StringComparer.Ordinal) ? texts["ui.dispatch.done." + done].Value : null, get, brand, errors));

        staff.MapPost("/dispatches/{dispatchId:guid}/bundles", async (HttpContext http, Guid dispatchId, [FromForm] Guid? bundle, [FromForm] string? number, AddBundleToOperationsDispatchUseCase add,
            GetOperationsDispatchUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Error? error = (await add.ExecuteAsync(OperationsCurrentUser.Context(http), new AddBundleToOperationsDispatchRequest { DispatchId = dispatchId, BundleId = bundle, BundleNumber = number },
                http.RequestAborted)).Error;
            return Step(http, dispatchId, "bundle", error) ?? await DispatchAsync(http, dispatchId, error, null, get, brand, errors);
        });

        staff.MapPost("/dispatches/{dispatchId:guid}/bundles/{bundleId:guid}/remove", async (HttpContext http, Guid dispatchId, Guid bundleId, RemoveBundleFromOperationsDispatchUseCase remove,
            GetOperationsDispatchUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Error? error = (await remove.ExecuteAsync(OperationsCurrentUser.Context(http), new RemoveBundleFromOperationsDispatchRequest { DispatchId = dispatchId, BundleId = bundleId },
                http.RequestAborted)).Error;
            return Step(http, dispatchId, "removed", error) ?? await DispatchAsync(http, dispatchId, error, null, get, brand, errors);
        });

        staff.MapPost("/dispatches/{dispatchId:guid}/legs", async (HttpContext http, Guid dispatchId, [FromForm] Guid? carrier, [FromForm] string? kind, [FromForm] string? from, [FromForm] string? to,
            [FromForm] string? departure, [FromForm] string? arrival, [FromForm] string? reference, [FromForm] string? driver, [FromForm] string? vehicle, [FromForm] string? key, AddOperationsDispatchLegUseCase add,
            GetOperationsDispatchUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Error? error = (await add.ExecuteAsync(OperationsCurrentUser.Context(http), new AddOperationsDispatchLegRequest
            {
                DispatchId = dispatchId,
                CarrierId = carrier,
                Kind = Enum.TryParse(kind, false, out OperationsCarrierKind parsed) && Enum.IsDefined(parsed) ? parsed : OperationsCarrierKind.Ground,
                From = from,
                To = to,
                Departure = Local(departure),
                Arrival = Local(arrival),
                Reference = reference,
                Driver = driver,
                Vehicle = vehicle,
                RequestKey = Guid.TryParseExact(key, "N", out Guid formKey) ? formKey : null
            }, http.RequestAborted)).Error;
            return Step(http, dispatchId, "leg", error) ?? await DispatchAsync(http, dispatchId, error, null, get, brand, errors);
        });

        staff.MapPost("/dispatches/{dispatchId:guid}/legs/{legId:guid}/remove", async (HttpContext http, Guid dispatchId, Guid legId, RemoveOperationsDispatchLegUseCase remove,
            GetOperationsDispatchUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Error? error = (await remove.ExecuteAsync(OperationsCurrentUser.Context(http), new RemoveOperationsDispatchLegRequest { DispatchId = dispatchId, LegId = legId }, http.RequestAborted)).Error;
            return Step(http, dispatchId, "removed", error) ?? await DispatchAsync(http, dispatchId, error, null, get, brand, errors);
        });

        staff.MapPost("/dispatches/{dispatchId:guid}/close", async (HttpContext http, Guid dispatchId, CloseOperationsDispatchUseCase close, GetOperationsDispatchUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Error? error = (await close.ExecuteAsync(OperationsCurrentUser.Context(http), new CloseOperationsDispatchRequest { DispatchId = dispatchId }, http.RequestAborted)).Error;
            return Step(http, dispatchId, "closed", error) ?? await DispatchAsync(http, dispatchId, error, null, get, brand, errors);
        });

        staff.MapPost("/dispatches/{dispatchId:guid}/legs/{legId:guid}/depart", async (HttpContext http, Guid dispatchId, Guid legId, [FromForm] string? at, DepartOperationsDispatchLegUseCase depart,
            GetOperationsDispatchUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Error? error = (await depart.ExecuteAsync(OperationsCurrentUser.Context(http), new DepartOperationsDispatchLegRequest { DispatchId = dispatchId, LegId = legId, At = Local(at) },
                http.RequestAborted)).Error;
            return Step(http, dispatchId, "departed", error) ?? await DispatchAsync(http, dispatchId, error, null, get, brand, errors);
        });

        staff.MapPost("/dispatches/{dispatchId:guid}/legs/{legId:guid}/arrive", async (HttpContext http, Guid dispatchId, Guid legId, [FromForm] string? at, ArriveOperationsDispatchLegUseCase arrive,
            GetOperationsDispatchUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Error? error = (await arrive.ExecuteAsync(OperationsCurrentUser.Context(http), new ArriveOperationsDispatchLegRequest { DispatchId = dispatchId, LegId = legId, At = Local(at) },
                http.RequestAborted)).Error;
            return Step(http, dispatchId, "arrived", error) ?? await DispatchAsync(http, dispatchId, error, null, get, brand, errors);
        });

        staff.MapPost("/dispatches/{dispatchId:guid}/cancel", async (HttpContext http, Guid dispatchId, [FromForm] string? reason, CancelOperationsDispatchUseCase cancel,
            GetOperationsDispatchUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Error? error = (await cancel.ExecuteAsync(OperationsCurrentUser.Context(http), new CancelOperationsDispatchRequest { DispatchId = dispatchId, Reason = reason }, http.RequestAborted)).Error;
            return Step(http, dispatchId, "cancelled", error) ?? await DispatchAsync(http, dispatchId, error, null, get, brand, errors);
        });

        staff.MapGet("/dispatches/{dispatchId:guid}/manifest", async (HttpContext http, Guid dispatchId, GetOperationsDispatchManifestUseCase manifest) =>
        {
            GetOperationsDispatchManifestResponse response = await manifest.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsDispatchManifestRequest { DispatchId = dispatchId },
                http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Pdf, "application/pdf", null) : Results.NotFound();
        });

        // Step 2: the cost, the letter of instruction, the house air waybills.
        staff.MapPost("/dispatches/{dispatchId:guid}/recost", async (HttpContext http, Guid dispatchId, RecostOperationsDispatchUseCase recost, GetOperationsDispatchUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
        {
            RecostOperationsDispatchResponse response = await recost.ExecuteAsync(OperationsCurrentUser.Context(http), new RecostOperationsDispatchRequest { DispatchId = dispatchId }, http.RequestAborted);

            if (response.Succeeded && response.LegsWithoutRate.Count > 0)
            {
                return await DispatchAsync(http, dispatchId, null, VixlyText.Format(texts, "ui.dispatch.cost.missing", "legs", string.Join(", ", response.LegsWithoutRate)), get, brand, errors);
            }

            return Step(http, dispatchId, "costed", response.Error) ?? await DispatchAsync(http, dispatchId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/dispatches/{dispatchId:guid}/cost", async (HttpContext http, Guid dispatchId, [FromForm] string? total, [FromForm] string? note, CorrectOperationsDispatchCostUseCase correct,
            GetOperationsDispatchUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Error? error = (await correct.ExecuteAsync(OperationsCurrentUser.Context(http), new CorrectOperationsDispatchCostRequest { DispatchId = dispatchId, Total = Amount(total), Note = note },
                http.RequestAborted)).Error;
            return Step(http, dispatchId, "cost", error) ?? await DispatchAsync(http, dispatchId, error, null, get, brand, errors);
        });

        staff.MapPost("/dispatches/{dispatchId:guid}/instructions", async (HttpContext http, Guid dispatchId, [FromForm] bool? collect, [FromForm] string? exportReference,
            [FromForm] string? handling, SaveOperationsDispatchInstructionsUseCase save, GetOperationsDispatchUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            OperationsDispatchDocuments chosen = OperationsDispatchDocuments.None;

            // Several checkboxes of one name.
            foreach (string? document in (await http.Request.ReadFormAsync(http.RequestAborted))["documents"])
            {
                if (Enum.TryParse(document, false, out OperationsDispatchDocuments one) && Enum.IsDefined(one) && one != OperationsDispatchDocuments.None)
                {
                    chosen |= one;
                }
            }

            Error? error = (await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsDispatchInstructionsRequest
            {
                DispatchId = dispatchId,
                ChargesCollect = collect == true,
                ExportReference = exportReference,
                Documents = chosen,
                HandlingInstructions = handling
            }, http.RequestAborted)).Error;
            return Step(http, dispatchId, "instructions", error) ?? await DispatchAsync(http, dispatchId, error, null, get, brand, errors);
        });

        staff.MapGet("/dispatches/{dispatchId:guid}/sli", async (HttpContext http, Guid dispatchId, GetOperationsShipperLetterUseCase letter) =>
        {
            GetOperationsShipperLetterResponse response = await letter.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsShipperLetterRequest { DispatchId = dispatchId }, http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Pdf, "application/pdf", null) : Results.NotFound();
        });

        staff.MapGet("/dispatches/{dispatchId:guid}/hawb", async (HttpContext http, Guid dispatchId, Guid? shipment, GetOperationsHouseWaybillsUseCase waybills, IStringLocalizer<OperationsText> texts) =>
        {
            GetOperationsHouseWaybillsResponse response = await waybills.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsHouseWaybillsRequest
            {
                DispatchId = dispatchId,
                ShipmentId = shipment,
                ContentsFallback = texts["ui.waybill.goods_fallback"].Value
            }, http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Pdf, "application/pdf", null) : Results.NotFound();
        });

        staff.MapGet("/bundles/{bundleId:guid}/manifest", async (HttpContext http, Guid bundleId, GetOperationsBundleManifestUseCase manifest) =>
        {
            GetOperationsBundleManifestResponse response = await manifest.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsBundleManifestRequest { BundleId = bundleId },
                http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Pdf, "application/pdf", null) : Results.NotFound();
        });

        staff.MapGet("/carriers", async (HttpContext http, string? done, GetOperationsCarriersUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await CarriersAsync(http, null, get, brand, errors));

        staff.MapPost("/carriers", async (HttpContext http, [FromForm] string? name, [FromForm] string? kind, [FromForm] string? account, [FromForm] string? prefix, SaveOperationsCarrierUseCase save,
            GetOperationsCarriersUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            SaveOperationsCarrierResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), Carrier(null, name, kind, account, prefix, true), http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/carriers/" + response.CarrierId + "?done=saved") : await CarriersAsync(http, response.Error, get, brand, errors);
        });

        staff.MapGet("/carriers/{carrierId:guid}", async (HttpContext http, Guid carrierId, string? done, GetOperationsCarrierUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
            await CarrierAsync(http, carrierId, null, done is "saved" or "rate" or "removed" ? texts["ui.carriers.done." + done].Value : null, get, brand, errors));

        staff.MapPost("/carriers/{carrierId:guid}", async (HttpContext http, Guid carrierId, [FromForm] string? name, [FromForm] string? kind, [FromForm] string? account, [FromForm] string? prefix,
            [FromForm] bool? active, SaveOperationsCarrierUseCase save, GetOperationsCarrierUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            SaveOperationsCarrierResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), Carrier(carrierId, name, kind, account, prefix, active == true), http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/carriers/" + carrierId + "?done=saved") : await CarrierAsync(http, carrierId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/carriers/{carrierId:guid}/rates", async (HttpContext http, Guid carrierId, [FromForm] string? destination, [FromForm] string? type, [FromForm] string? perWeight,
            [FromForm] string? minimum, [FromForm] string? perBundle, [FromForm] string? starts, AddOperationsCarrierRateUseCase add, GetOperationsCarrierUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            AddOperationsCarrierRateResponse response = await add.ExecuteAsync(OperationsCurrentUser.Context(http), new AddOperationsCarrierRateRequest
            {
                CarrierId = carrierId,
                DestinationWarehouseId = destination is { Length: 34 } && destination.StartsWith("w:", StringComparison.Ordinal) && Guid.TryParseExact(destination[2..], "N", out Guid to) ? to : null,
                CountryCode = destination is { Length: 4 } && destination.StartsWith("c:", StringComparison.Ordinal) ? destination[2..] : null,
                BundleType = type,
                PerWeight = Amount(perWeight),
                Minimum = Amount(minimum),
                PerBundle = Amount(perBundle),
                StartsOn = DateOnly.TryParseExact(starts, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day) ? day : DateOnly.MinValue
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/carriers/" + carrierId + "?done=rate") : await CarrierAsync(http, carrierId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/carriers/{carrierId:guid}/rates/{rateId:guid}/remove", async (HttpContext http, Guid carrierId, Guid rateId, RemoveOperationsCarrierRateUseCase remove,
            GetOperationsCarrierUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            RemoveOperationsCarrierRateResponse response = await remove.ExecuteAsync(OperationsCurrentUser.Context(http), new RemoveOperationsCarrierRateRequest { RateId = rateId }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/carriers/" + carrierId + "?done=removed") : await CarrierAsync(http, carrierId, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    // A datetime-local field: where the company works.
    public static DateTime? Local(string? typed) =>
        DateTime.TryParseExact(typed?.Trim(), new[] { "yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed) ? parsed : null;

    // Empty is none; anything unreadable is refused by the domain.
    private static decimal? Amount(string? typed) => string.IsNullOrWhiteSpace(typed) ? null : OperationsCounterEndpoints.Amount(typed) ?? -1m;

    private static SaveOperationsCarrierRequest Carrier(Guid? carrierId, string? name, string? kind, string? account, string? prefix, bool active) => new()
    {
        CarrierId = carrierId,
        Name = name,
        Kind = Enum.TryParse(kind, false, out OperationsCarrierKind parsed) && Enum.IsDefined(parsed) ? parsed : (OperationsCarrierKind)0,
        AccountNumber = account,
        AirlinePrefix = prefix,
        Active = active
    };

    // Done: back to the dispatch (a refresh posts nothing); refused: the page with the error.
    private static IResult? Step(HttpContext http, Guid dispatchId, string done, Error? error) =>
        error is null ? Results.Redirect("/staff/dispatches/" + dispatchId + "?done=" + done) : null;

    private static async Task<IResult> DeskAsync(HttpContext http, string? view, Error? error, GetOperationsDispatchesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsDispatchesResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsDispatchesRequest { View = view }, http.RequestAborted);
        return OperationsStaffResults.Page<DispatchesPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Warehouses,
            response.Destinations,
            response.Dispatches,
            response.Services,
            response.FreeBundles,
            response.TimeZone,
            All = view == "all",
            Error = errors.Describe(error ?? response.Error)
        });
    }

    private static async Task<IResult> DispatchAsync(HttpContext http, Guid dispatchId, Error? error, string? info, GetOperationsDispatchUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsDispatchResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsDispatchRequest { DispatchId = dispatchId }, http.RequestAborted);

        if (!response.Succeeded)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<DispatchPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Dispatch,
            response.Warehouse,
            response.Destination,
            response.Bundles,
            response.Candidates,
            response.Carriers,
            response.WeightUnit,
            response.TimeZone,
            response.Shipments,
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static async Task<IResult> CarriersAsync(HttpContext http, Error? error, GetOperationsCarriersUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsCarriersResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCarriersRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<CarriersPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Carriers,
            Error = errors.Describe(error ?? response.Error)
        });
    }

    private static async Task<IResult> CarrierAsync(HttpContext http, Guid carrierId, Error? error, string? info, GetOperationsCarrierUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsCarrierResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCarrierRequest { CarrierId = carrierId }, http.RequestAborted);

        if (!response.Succeeded)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<CarrierPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Carrier,
            response.Rates,
            response.Destinations,
            response.BundleTypes,
            response.CurrencyCode,
            response.WeightUnit,
            response.Today,
            Error = errors.Describe(error),
            Info = info
        });
    }
}
