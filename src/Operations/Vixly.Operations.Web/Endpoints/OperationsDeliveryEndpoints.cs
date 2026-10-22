using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Delivery.BillOperationsDelivery;
using Vixly.Operations.Application.UseCases.Delivery.CancelOperationsDeliveryRoute;
using Vixly.Operations.Application.UseCases.Delivery.ChangeOperationsDeliveryRoute;
using Vixly.Operations.Application.UseCases.Delivery.GetMyOperationsDeliveryRoutes;
using Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliveries;
using Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliveryRoute;
using Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliverySettings;
using Vixly.Operations.Application.UseCases.Delivery.GetOperationsRunSheet;
using Vixly.Operations.Application.UseCases.Delivery.PlanOperationsDeliveryRoute;
using Vixly.Operations.Application.UseCases.Delivery.RecordOperationsDeliveryStop;
using Vixly.Operations.Application.UseCases.Delivery.ReturnOperationsDeliveryParcel;
using Vixly.Operations.Application.UseCases.Delivery.SaveOperationsDeliverySettings;
using Vixly.Operations.Application.UseCases.Delivery.SettleOperationsDeliveryRoute;
using Vixly.Operations.Application.UseCases.Delivery.StartOperationsDeliveryRoute;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Delivery;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsDeliveryEndpoints.cs
//
// PURPOSE:
// Home delivery (Slice 7 step 3): the delivery desk of a warehouse, a route with its stops and run sheet, a driver's routes, Settings → Delivery.
//
// RESPONSIBILITIES:
// - /staff/delivery (plan, return to sender), /staff/delivery/routes/{id} (change, start, record, settle, cancel, run sheet), /staff/drive, /staff/settings/delivery
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); the use cases check every permission
// - The signature arrives as a PNG data address, the photo as a file; both are checked again by the use case
// =========================================================
public static class OperationsDeliveryEndpoints
{
    private const int SignatureFieldMax = 300_000;

    private const long PhotoMaxBytes = 10 * 1024 * 1024;

    public static IEndpointRouteBuilder MapOperationsDeliveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/delivery", async (HttpContext http, Guid? warehouse, string? done, GetOperationsDeliveriesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> texts) =>
            await DeskAsync(http, warehouse, null, done is "returned" or "billed" ? texts["ui.delivery.done." + done].Value : null, get, brand, errors));

        staff.MapPost("/delivery", async (HttpContext http, PlanOperationsDeliveryRouteUseCase plan, GetOperationsDeliveriesUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            Guid? warehouse = Guid.TryParse(form["warehouse"].ToString(), out Guid at) ? at : null;

            if (!DateOnly.TryParseExact(form["date"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            {
                return await DeskAsync(http, warehouse, OperationsDeliveryErrors.DateInvalid, null, get, brand, errors);
            }

            PlanOperationsDeliveryRouteResponse response = await plan.ExecuteAsync(OperationsCurrentUser.Context(http), new PlanOperationsDeliveryRouteRequest
            {
                WarehouseId = warehouse ?? Guid.Empty,
                ServiceDate = date,
                DriverUserId = Guid.TryParse(form["driver"].ToString(), out Guid driver) ? driver : null,
                DriverName = form["driverName"].ToString(),
                ShipmentIds = Ids(form["shipments"]),
                RequestKey = Guid.TryParseExact(form["key"].ToString(), "N", out Guid key) ? key : null
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/delivery/routes/" + response.RouteId + "?done=planned")
                : await DeskAsync(http, warehouse, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/delivery/bill", async (HttpContext http, [FromForm] string? warehouse, [FromForm] string? shipment, [FromForm] string? duties, [FromForm] string? localTax,
            [FromForm] string? localFee, [FromForm] string? key, BillOperationsDeliveryUseCase bill, GetOperationsDeliveriesUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
        {
            Guid? at = Guid.TryParse(warehouse, out Guid parsed) ? parsed : null;

            if (!Amount(duties, out decimal? dutiesAmount) || !Amount(localTax, out decimal? taxAmount) || !Amount(localFee, out decimal? feeAmount))
            {
                return await DeskAsync(http, at, OperationsPickupErrors.AmountInvalid, null, get, brand, errors);
            }

            BillOperationsDeliveryResponse response = await bill.ExecuteAsync(OperationsCurrentUser.Context(http), new BillOperationsDeliveryRequest
            {
                WarehouseId = at ?? Guid.Empty, ShipmentId = Guid.TryParse(shipment, out Guid shipmentId) ? shipmentId : Guid.Empty, Duties = dutiesAmount, LocalTax = taxAmount,
                LocalFee = feeAmount, RequestKey = Guid.TryParseExact(key, "N", out Guid billKey) ? billKey : null
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/delivery?warehouse=" + at + "&done=billed")
                : await DeskAsync(http, at, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/delivery/returns/{parcelId:guid}", async (HttpContext http, Guid parcelId, [FromForm] string? warehouse, [FromForm] string? note, ReturnOperationsDeliveryParcelUseCase send,
            GetOperationsDeliveriesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Guid? at = Guid.TryParse(warehouse, out Guid parsed) ? parsed : null;
            ReturnOperationsDeliveryParcelResponse response = await send.ExecuteAsync(OperationsCurrentUser.Context(http), new ReturnOperationsDeliveryParcelRequest
            {
                WarehouseId = at ?? Guid.Empty, ParcelId = parcelId, Note = note
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/delivery?warehouse=" + at + "&done=returned") : await DeskAsync(http, at, response.Error, null, get, brand, errors);
        });

        staff.MapGet("/delivery/routes/{routeId:guid}", async (HttpContext http, Guid routeId, string? done, GetOperationsDeliveryRouteUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
            await RouteAsync(http, routeId, null, done is "planned" or "changed" or "started" or "recorded" or "settled" or "cancelled" ? texts["ui.delivery.done." + done].Value : null, get, brand,
                errors));

        staff.MapGet("/delivery/routes/{routeId:guid}/sheet", async (HttpContext http, Guid routeId, GetOperationsRunSheetUseCase sheet) =>
        {
            GetOperationsRunSheetResponse response = await sheet.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsRunSheetRequest { RouteId = routeId }, http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Pdf, "application/pdf", null) : Results.NotFound();
        });

        staff.MapPost("/delivery/routes/{routeId:guid}/stops", async (HttpContext http, Guid routeId, ChangeOperationsDeliveryRouteUseCase change, GetOperationsDeliveryRouteUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            Guid? stop = Guid.TryParse(form["stop"].ToString(), out Guid stopId) ? stopId : null;
            string action = form["action"].ToString();
            ChangeOperationsDeliveryRouteResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http), new ChangeOperationsDeliveryRouteRequest
            {
                RouteId = routeId,
                AddShipmentIds = action == "add" ? Ids(form["shipments"]) : Array.Empty<Guid>(),
                RemoveStopId = action == "remove" ? stop : null,
                MoveStopId = action is "up" or "down" ? stop : null,
                Up = action == "up"
            }, http.RequestAborted);
            return Step(routeId, "changed", response.Error) ?? await RouteAsync(http, routeId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/delivery/routes/{routeId:guid}/start", async (HttpContext http, Guid routeId, StartOperationsDeliveryRouteUseCase start, GetOperationsDeliveryRouteUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Error? error = (await start.ExecuteAsync(OperationsCurrentUser.Context(http), new StartOperationsDeliveryRouteRequest
            {
                RouteId = routeId, PortalSlug = OperationsStaffResults.PortalSlug(http)
            }, http.RequestAborted)).Error;
            return Step(routeId, "started", error) ?? await RouteAsync(http, routeId, error, null, get, brand, errors);
        });

        staff.MapPost("/delivery/routes/{routeId:guid}/cancel", async (HttpContext http, Guid routeId, [FromForm] string? reason, CancelOperationsDeliveryRouteUseCase cancel,
            GetOperationsDeliveryRouteUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Error? error = (await cancel.ExecuteAsync(OperationsCurrentUser.Context(http), new CancelOperationsDeliveryRouteRequest { RouteId = routeId, Reason = reason }, http.RequestAborted)).Error;
            return Step(routeId, "cancelled", error) ?? await RouteAsync(http, routeId, error, null, get, brand, errors);
        });

        staff.MapPost("/delivery/routes/{routeId:guid}/settle", async (HttpContext http, Guid routeId, [FromForm] string? handedIn, [FromForm] string? note,
            SettleOperationsDeliveryRouteUseCase settle, GetOperationsDeliveryRouteUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            if (!Amount(handedIn, out decimal? amount) || amount is null)
            {
                return await RouteAsync(http, routeId, OperationsDeliveryErrors.CashInvalid, null, get, brand, errors);
            }

            Error? error = (await settle.ExecuteAsync(OperationsCurrentUser.Context(http), new SettleOperationsDeliveryRouteRequest
            {
                RouteId = routeId, HandedIn = amount.Value, Note = note
            }, http.RequestAborted)).Error;
            return Step(routeId, "settled", error) ?? await RouteAsync(http, routeId, error, null, get, brand, errors);
        });

        staff.MapPost("/delivery/routes/{routeId:guid}/stops/{stopId:guid}", async (HttpContext http, Guid routeId, Guid stopId, RecordOperationsDeliveryStopUseCase record,
            GetOperationsDeliveryRouteUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            byte[]? signature = null;
            string drawn = form["signature"].ToString();

            if (drawn.Length is > 0 and <= SignatureFieldMax && drawn.StartsWith("data:image/png;base64,", StringComparison.Ordinal))
            {
                try
                {
                    signature = Convert.FromBase64String(drawn["data:image/png;base64,".Length..]);
                }
                catch (FormatException)
                {
                    signature = Array.Empty<byte>();
                }
            }
            else if (drawn.Length > 0)
            {
                signature = Array.Empty<byte>();
            }

            byte[]? photo = null;

            if (form.Files.GetFile("photo") is { Length: > 0 } file)
            {
                if (file.Length > PhotoMaxBytes)
                {
                    return await RouteAsync(http, routeId, OperationsPickupErrors.PhotoRequired, null, get, brand, errors);
                }

                using MemoryStream buffer = new();
                await file.CopyToAsync(buffer, http.RequestAborted);
                photo = buffer.ToArray();
            }

            if (!Amount(form["cash"].ToString(), out decimal? cash))
            {
                return await RouteAsync(http, routeId, OperationsDeliveryErrors.CashInvalid, null, get, brand, errors);
            }

            RecordOperationsDeliveryStopResponse response = await record.ExecuteAsync(OperationsCurrentUser.Context(http), new RecordOperationsDeliveryStopRequest
            {
                RouteId = routeId,
                StopId = stopId,
                Delivered = form["outcome"].ToString() == "delivered",
                ReceiverName = form["receiver"].ToString(),
                IdKind = Enum.TryParse(form["idKind"].ToString(), false, out OperationsPickupIdKind idKind) && Enum.IsDefined(idKind) ? idKind : null,
                IdLast = form["idLast"].ToString(),
                Signature = signature is { Length: > 0 } ? signature : signature is null ? null : new byte[] { 0 },
                Photo = photo,
                Code = form["code"].ToString(),
                Cash = cash,
                Failure = form["failure"].ToString(),
                Note = form["note"].ToString(),
                PortalSlug = OperationsStaffResults.PortalSlug(http)
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/delivery/routes/" + routeId + "?done=recorded#stop-" + stopId.ToString("N"))
                : await RouteAsync(http, routeId, response.Error, null, get, brand, errors);
        }).WithMetadata(new Vixly.Operations.Web.Hosting.OperationsUploadLimit(PhotoMaxBytes + SignatureFieldMax + (64 * 1024)))
            .WithFormOptions(multipartBodyLengthLimit: PhotoMaxBytes + SignatureFieldMax + (64 * 1024));

        staff.MapGet("/drive", async (HttpContext http, GetMyOperationsDeliveryRoutesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            GetMyOperationsDeliveryRoutesResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetMyOperationsDeliveryRoutesRequest(), http.RequestAborted);
            return OperationsStaffResults.Page<DrivePage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                response.Routes,
                Error = errors.Describe(response.Error)
            });
        });

        staff.MapGet("/settings/delivery", async (HttpContext http, string? done, GetOperationsDeliverySettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> texts) =>
            await SettingsAsync(http, null, done == "1" ? texts["ui.staff.done.saved"].Value : null, get, brand, errors));

        staff.MapPost("/settings/delivery", async (HttpContext http, SaveOperationsDeliverySettingsUseCase save, GetOperationsDeliverySettingsUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            OperationsDeliveryProofs proofs = OperationsDeliveryProofs.None;

            foreach (string? value in form["proofs"])
            {
                if (Enum.TryParse(value, false, out OperationsDeliveryProofs proof) && proof is OperationsDeliveryProofs.Signature or OperationsDeliveryProofs.Photo or OperationsDeliveryProofs.Id
                    or OperationsDeliveryProofs.Code)
                {
                    proofs |= proof;
                }
            }

            if (!int.TryParse(form["tries"].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int tries)
                || !Enum.TryParse(form["recording"].ToString(), false, out OperationsDeliveryRecording recording)
                || !Enum.TryParse(form["afterFailed"].ToString(), false, out OperationsAfterFailedDelivery afterFailed))
            {
                return await SettingsAsync(http, OperationsDeliveryErrors.SettingsInvalid, null, get, brand, errors);
            }

            SaveOperationsDeliverySettingsResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsDeliverySettingsRequest
            {
                Recording = recording, CashOnDelivery = form["cash"].ToString() == "true", Tries = tries, AfterFailed = afterFailed, Proofs = proofs, Emails = form["emails"].ToString() == "true"
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/delivery?done=1") : await SettingsAsync(http, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    private static IReadOnlyList<Guid> Ids(Microsoft.Extensions.Primitives.StringValues values) =>
        values.Select(value => Guid.TryParse(value, out Guid id) ? id : Guid.Empty).Where(id => id != Guid.Empty).Distinct().Take(200).ToList();

    private static IResult? Step(Guid routeId, string done, Error? error) => error is null ? Results.Redirect("/staff/delivery/routes/" + routeId + "?done=" + done) : null;

    // An amount typed with a dot, or nothing.
    private static bool Amount(string? text, out decimal? amount)
    {
        amount = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
        {
            amount = value;
            return true;
        }

        return false;
    }

    private static async Task<IResult> DeskAsync(HttpContext http, Guid? warehouse, Error? error, string? info, GetOperationsDeliveriesUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsDeliveriesResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsDeliveriesRequest { WarehouseId = warehouse }, http.RequestAborted);
        return OperationsStaffResults.Page<DeliveriesPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Warehouses,
            response.WarehouseId,
            response.Today,
            response.Waiting,
            response.Routes,
            response.Returns,
            response.Drivers,
            response.Recording,
            response.CashOnDelivery,
            response.DestinationCharges,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> RouteAsync(HttpContext http, Guid routeId, Error? error, string? info, GetOperationsDeliveryRouteUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsDeliveryRouteResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsDeliveryRouteRequest { RouteId = routeId }, http.RequestAborted);

        if (!response.Succeeded && error is null)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<DeliveryRoutePage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            RouteId = routeId,
            response.Number,
            response.Status,
            response.ServiceDate,
            response.DriverName,
            response.WarehouseName,
            response.TimeZone,
            response.Planner,
            response.CanRecord,
            response.Stops,
            response.Waiting,
            response.CashCollected,
            response.CashHandedIn,
            response.SettleNote,
            response.CancelReason,
            response.CashPaidIn,
            response.Proofs,
            response.Recording,
            response.CashOnDelivery,
            response.CurrencyCode,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> SettingsAsync(HttpContext http, Error? error, string? info, GetOperationsDeliverySettingsUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsDeliverySettingsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsDeliverySettingsRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<DeliverySettingsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Recording,
            response.CashOnDelivery,
            response.Tries,
            response.AfterFailed,
            response.Proofs,
            response.Emails,
            response.CanEdit,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
