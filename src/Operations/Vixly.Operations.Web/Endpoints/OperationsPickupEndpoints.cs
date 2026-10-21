using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Pickup.BillOperationsPickup;
using Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickup;
using Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickups;
using Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickupSettings;
using Vixly.Operations.Application.UseCases.Pickup.ReleaseOperationsPickup;
using Vixly.Operations.Application.UseCases.Pickup.SaveOperationsPickupSettings;
using Vixly.Operations.Application.UseCases.Pickup.SendOperationsPickupCode;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Pickup;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsPickupEndpoints.cs
//
// PURPOSE:
// The pickup counter (Slice 7 step 2): the shipments ready, a shipment's counter page with billing, the code and the release; Settings → Pickup.
//
// RESPONSIBILITIES:
// - /staff/pickup, /staff/pickup/{shipmentId} with bill, code and release; /staff/settings/pickup
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); the use cases check every permission
// - The signature arrives as a PNG data address, the photo as a file; both are checked again by the use case
// =========================================================
public static class OperationsPickupEndpoints
{
    private const int SignatureFieldMax = 300_000;

    private const long PhotoMaxBytes = 10 * 1024 * 1024;

    public static IEndpointRouteBuilder MapOperationsPickupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/pickup", async (HttpContext http, Guid? warehouse, string? query, GetOperationsPickupsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            GetOperationsPickupsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsPickupsRequest { WarehouseId = warehouse, Query = query },
                http.RequestAborted);
            return OperationsStaffResults.Page<PickupsPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                response.Warehouses,
                response.WarehouseId,
                Query = query,
                response.Rows,
                Error = errors.Describe(response.Error)
            });
        });

        staff.MapGet("/pickup/{shipmentId:guid}", async (HttpContext http, Guid shipmentId, Guid? warehouse, string? done, GetOperationsPickupUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
            await PickupAsync(http, shipmentId, warehouse ?? Guid.Empty, null, done is "billed" or "code" or "released" ? texts["ui.pickup.done." + done].Value : null, get, brand, errors));

        staff.MapPost("/pickup/{shipmentId:guid}/bill", async (HttpContext http, Guid shipmentId, [FromForm] Guid? warehouse, [FromForm] string? duties, [FromForm] string? localTax,
            [FromForm] string? localFee, [FromForm] string? key, BillOperationsPickupUseCase bill, GetOperationsPickupUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            if (!Amount(duties, out decimal? dutiesAmount) || !Amount(localTax, out decimal? taxAmount) || !Amount(localFee, out decimal? feeAmount))
            {
                return await PickupAsync(http, shipmentId, warehouse ?? Guid.Empty, OperationsPickupErrors.AmountInvalid, null, get, brand, errors);
            }

            BillOperationsPickupResponse response = await bill.ExecuteAsync(OperationsCurrentUser.Context(http), new BillOperationsPickupRequest
            {
                ShipmentId = shipmentId, WarehouseId = warehouse ?? Guid.Empty, Duties = dutiesAmount, LocalTax = taxAmount, LocalFee = feeAmount,
                RequestKey = Guid.TryParseExact(key, "N", out Guid billKey) ? billKey : null
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/pickup/" + shipmentId + "?warehouse=" + warehouse + "&done=billed")
                : await PickupAsync(http, shipmentId, warehouse ?? Guid.Empty, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/pickup/{shipmentId:guid}/code", async (HttpContext http, Guid shipmentId, [FromForm] Guid? warehouse, SendOperationsPickupCodeUseCase send,
            GetOperationsPickupUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            SendOperationsPickupCodeResponse response = await send.ExecuteAsync(OperationsCurrentUser.Context(http), new SendOperationsPickupCodeRequest
            {
                ShipmentId = shipmentId, WarehouseId = warehouse ?? Guid.Empty, PortalSlug = OperationsStaffResults.PortalSlug(http)
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/pickup/" + shipmentId + "?warehouse=" + warehouse + "&done=code")
                : await PickupAsync(http, shipmentId, warehouse ?? Guid.Empty, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/pickup/{shipmentId:guid}/release", async (HttpContext http, Guid shipmentId, ReleaseOperationsPickupUseCase release, GetOperationsPickupUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            Guid warehouse = Guid.TryParse(form["warehouse"].ToString(), out Guid at) ? at : Guid.Empty;
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
                    return await PickupAsync(http, shipmentId, warehouse, OperationsPickupErrors.PhotoRequired, null, get, brand, errors);
                }

                using MemoryStream buffer = new();
                await file.CopyToAsync(buffer, http.RequestAborted);
                photo = buffer.ToArray();
            }

            ReleaseOperationsPickupResponse response = await release.ExecuteAsync(OperationsCurrentUser.Context(http), new ReleaseOperationsPickupRequest
            {
                ShipmentId = shipmentId,
                WarehouseId = warehouse,
                Collector = Enum.TryParse(form["collector"].ToString(), false, out OperationsPickupCollector collector) && Enum.IsDefined(collector) ? collector : (OperationsPickupCollector)0,
                IdKind = Enum.TryParse(form["idKind"].ToString(), false, out OperationsPickupIdKind idKind) && Enum.IsDefined(idKind) ? idKind : null,
                IdLast = form["idLast"].ToString(),
                Signature = signature is { Length: > 0 } ? signature : signature is null ? null : new byte[] { 0 },
                Photo = photo,
                Code = form["code"].ToString(),
                RequestKey = Guid.TryParseExact(form["key"].ToString(), "N", out Guid key) ? key : null
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/pickup/" + shipmentId + "?warehouse=" + warehouse + "&done=released")
                : await PickupAsync(http, shipmentId, warehouse, response.Error, null, get, brand, errors);
        }).WithMetadata(new Vixly.Operations.Web.Hosting.OperationsUploadLimit(PhotoMaxBytes + SignatureFieldMax + (64 * 1024)))
            .WithFormOptions(multipartBodyLengthLimit: PhotoMaxBytes + SignatureFieldMax + (64 * 1024));

        staff.MapGet("/settings/pickup", async (HttpContext http, string? done, GetOperationsPickupSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> texts) =>
            await SettingsAsync(http, null, done == "1" ? texts["ui.staff.done.saved"].Value : null, get, brand, errors));

        staff.MapPost("/settings/pickup", async (HttpContext http, [FromForm] bool? charges, [FromForm] bool? storage, [FromForm] string? freeDays, [FromForm] string? fee,
            SaveOperationsPickupSettingsUseCase save, GetOperationsPickupSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsPickupProofs proofs = OperationsPickupProofs.None;

            foreach (string? value in (await http.Request.ReadFormAsync(http.RequestAborted))["proofs"])
            {
                if (Enum.TryParse(value, false, out OperationsPickupProofs proof) && proof is OperationsPickupProofs.Id or OperationsPickupProofs.Signature or OperationsPickupProofs.Code
                    or OperationsPickupProofs.Photo)
                {
                    proofs |= proof;
                }
            }

            if (!int.TryParse(freeDays, NumberStyles.Integer, CultureInfo.InvariantCulture, out int days) || !Amount(fee, out decimal? perDay))
            {
                return await SettingsAsync(http, OperationsPickupErrors.SettingsInvalid, null, get, brand, errors);
            }

            SaveOperationsPickupSettingsResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsPickupSettingsRequest
            {
                Proofs = proofs, DestinationCharges = charges == true, DestinationStorage = storage == true, FreeDays = days, FeePerDay = perDay
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/pickup?done=1") : await SettingsAsync(http, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

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

    private static async Task<IResult> PickupAsync(HttpContext http, Guid shipmentId, Guid warehouseId, Error? error, string? info, GetOperationsPickupUseCase get,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsPickupResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsPickupRequest { ShipmentId = shipmentId, WarehouseId = warehouseId },
            http.RequestAborted);

        if (!response.Succeeded && error is null)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<PickupPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            ShipmentId = shipmentId,
            response.Number,
            response.CustomerName,
            response.CustomerPhone,
            response.RecipientName,
            response.RecipientPhone,
            response.CollectorName,
            response.CollectorPhone,
            response.Here,
            response.Parcels,
            response.Invoices,
            response.Releases,
            response.StorageOwed,
            response.CurrencyCode,
            response.Proofs,
            response.DestinationCharges,
            response.CanBill,
            response.CodeSent,
            response.PayFirst,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> SettingsAsync(HttpContext http, Error? error, string? info, GetOperationsPickupSettingsUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsPickupSettingsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsPickupSettingsRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<PickupSettingsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Proofs,
            response.DestinationCharges,
            response.DestinationStorage,
            response.FreeDays,
            response.FeePerDay,
            response.CurrencyCode,
            response.CanEdit,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
