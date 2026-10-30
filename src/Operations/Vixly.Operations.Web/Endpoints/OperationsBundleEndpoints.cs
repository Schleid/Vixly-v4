using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundles;
using Vixly.Operations.Application.UseCases.Bundles.AddToOperationsBundleList;
using Vixly.Operations.Application.UseCases.Bundles.RemoveFromOperationsBundleList;
using Vixly.Operations.Application.UseCases.Bundles.CreateOperationsBundle;
using Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundle;
using Vixly.Operations.Application.UseCases.Bundles.FindOperationsBundleCode;
using Vixly.Operations.Application.UseCases.Bundles.ScanIntoOperationsBundle;
using Vixly.Operations.Application.UseCases.Bundles.TakeOutOfOperationsBundle;
using Vixly.Operations.Application.UseCases.Bundles.CloseOperationsBundle;
using Vixly.Operations.Application.UseCases.Bundles.VoidOperationsBundle;
using Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundleLabel;
using Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBoxes;
using Vixly.Operations.Application.UseCases.Bundles.ChooseOperationsConsolidation;
using Vixly.Operations.Application.UseCases.Bundles.OpenOperationsRepackBox;
using Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBox;
using Vixly.Operations.Application.UseCases.Bundles.ScanIntoOperationsRepackBox;
using Vixly.Operations.Application.UseCases.Bundles.TakeOutOfOperationsRepackBox;
using Vixly.Operations.Application.UseCases.Bundles.CloseOperationsRepackBox;
using Vixly.Operations.Application.UseCases.Bundles.VoidOperationsRepackBox;
using Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBoxLabel;
using Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundleSettings;
using Vixly.Operations.Application.UseCases.Bundles.SaveOperationsBundleSettings;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Bundles;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsBundleEndpoints.cs
//
// PURPOSE:
// Bundles and repack boxes (Slice 6A): the desk, a bundle, the fill station, consolidated shipments and their boxes, Settings → Bundles.
//
// RESPONSIBILITIES:
// - /staff/bundles and its forms; /staff/bundles/fill; /staff/bundles/boxes; /staff/settings/bundles
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); a scan posts and comes back to the station, ready for the next
// - The use cases check every permission
// =========================================================
public static class OperationsBundleEndpoints
{
    public static IEndpointRouteBuilder MapOperationsBundleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/bundles", async (HttpContext http, string? done, GetOperationsBundlesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> texts) =>
            await DeskAsync(http, null, done == "voided" ? texts["ui.bundles.done.voided"].Value : null, get, brand, errors));

        staff.MapPost("/bundles", async (HttpContext http, [FromForm] Guid? warehouse, [FromForm] string? destination, [FromForm] string? service, [FromForm] string? type,
            [FromForm] string? basis, [FromForm] string? target, [FromForm] string? key, CreateOperationsBundleUseCase create, GetOperationsBundlesUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            decimal? weight = string.IsNullOrWhiteSpace(target) ? null : OperationsCounterEndpoints.Amount(target) ?? -1m;
            CreateOperationsBundleResponse response = await create.ExecuteAsync(OperationsCurrentUser.Context(http), new CreateOperationsBundleRequest
            {
                WarehouseId = warehouse ?? Guid.Empty,
                DestinationWarehouseId = destination is { Length: 34 } && destination.StartsWith("w:", StringComparison.Ordinal) && Guid.TryParseExact(destination[2..], "N", out Guid to) ? to : null,
                CountryCode = destination is { Length: 4 } && destination.StartsWith("c:", StringComparison.Ordinal) ? destination[2..] : null,
                ServiceCode = service,
                TypeName = type,
                Basis = Enum.TryParse(basis, false, out OperationsBundleBasis parsed) && Enum.IsDefined(parsed) ? parsed : OperationsBundleBasis.Free,
                Target = weight,
                RequestKey = Guid.TryParseExact(key, "N", out Guid formKey) ? formKey : null
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bundles/" + response.BundleId + "?done=created") : await DeskAsync(http, response.Error, null, get, brand, errors);
        });

        staff.MapGet("/bundles/{bundleId:guid}", async (HttpContext http, Guid bundleId, string? done, GetOperationsBundleUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
            await BundleAsync(http, bundleId, null, done is "created" or "closed" or "listed" ? texts["ui.bundles.done." + done].Value : null, get, brand, errors));

        // The bundle's list from the computer (owner, 2026-10-21).
        staff.MapPost("/bundles/{bundleId:guid}/list", async (HttpContext http, Guid bundleId, AddToOperationsBundleListUseCase add, GetOperationsBundleUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            AddToOperationsBundleListResponse response = await add.ExecuteAsync(OperationsCurrentUser.Context(http), new AddToOperationsBundleListRequest
            {
                BundleId = bundleId,
                Ids = form["items"].Select(value => Guid.TryParse(value, out Guid id) ? id : Guid.Empty).Where(id => id != Guid.Empty).Distinct().Take(500).ToList()
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bundles/" + bundleId + "?done=listed") : await BundleAsync(http, bundleId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/bundles/{bundleId:guid}/list/{itemId:guid}/remove", async (HttpContext http, Guid bundleId, Guid itemId, RemoveFromOperationsBundleListUseCase remove,
            GetOperationsBundleUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            RemoveFromOperationsBundleListResponse response = await remove.ExecuteAsync(OperationsCurrentUser.Context(http), new RemoveFromOperationsBundleListRequest
            {
                BundleId = bundleId, ItemId = itemId
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bundles/" + bundleId) : await BundleAsync(http, bundleId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/bundles/{bundleId:guid}/close", async (HttpContext http, Guid bundleId, [FromForm] string? weight, [FromForm] string? length, [FromForm] string? width,
            [FromForm] string? height, CloseOperationsBundleUseCase close, GetOperationsBundleUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            CloseOperationsBundleResponse response = await close.ExecuteAsync(OperationsCurrentUser.Context(http), new CloseOperationsBundleRequest
            {
                BundleId = bundleId,
                Weight = Size(weight),
                Length = Size(length),
                Width = Size(width),
                Height = Size(height)
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bundles/" + bundleId + "?done=closed") : await BundleAsync(http, bundleId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/bundles/{bundleId:guid}/void", async (HttpContext http, Guid bundleId, [FromForm] string? reason, VoidOperationsBundleUseCase voiding, GetOperationsBundleUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            VoidOperationsBundleResponse response = await voiding.ExecuteAsync(OperationsCurrentUser.Context(http), new VoidOperationsBundleRequest { BundleId = bundleId, Reason = reason },
                http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bundles?done=voided") : await BundleAsync(http, bundleId, response.Error, null, get, brand, errors);
        });

        staff.MapGet("/bundles/{bundleId:guid}/label", async (HttpContext http, Guid bundleId, GetOperationsBundleLabelUseCase label) =>
        {
            GetOperationsBundleLabelResponse response = await label.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsBundleLabelRequest { BundleId = bundleId }, http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Pdf, "application/pdf", null) : Results.NotFound();
        });

        // The fill station: the bundle's (or a repack box's) barcode first.
        staff.MapGet("/bundles/fill", async (HttpContext http, GetOperationsSignInBrandUseCase brand) =>
            OperationsStaffResults.Page<FillBundlePage>(http, new { Company = await OperationsStaffResults.CompanyAsync(http, brand) }));

        staff.MapPost("/bundles/fill", async (HttpContext http, [FromForm] string? code, FindOperationsBundleCodeUseCase find, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            FindOperationsBundleCodeResponse response = await find.ExecuteAsync(OperationsCurrentUser.Context(http), new FindOperationsBundleCodeRequest { Code = code }, http.RequestAborted);

            if (response.Succeeded)
            {
                return Results.Redirect(response.BundleId is { } bundleId ? "/staff/bundles/" + bundleId + "/fill" : "/staff/bundles/boxes/" + response.BoxId);
            }

            return OperationsStaffResults.Page<FillBundlePage>(http, new { Company = await OperationsStaffResults.CompanyAsync(http, brand), Error = errors.Describe(response.Error) });
        });

        staff.MapGet("/bundles/{bundleId:guid}/fill", async (HttpContext http, Guid bundleId, Guid? parcel, string? note, GetOperationsBundleUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
            await FillAsync(http, bundleId, parcel, null, Note(texts, note), get, brand, errors));

        staff.MapPost("/bundles/{bundleId:guid}/scan", async (HttpContext http, Guid bundleId, [FromForm] string? code, [FromForm] string? key, ScanIntoOperationsBundleUseCase scan,
            GetOperationsBundleUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ScanIntoOperationsBundleResponse response = await scan.ExecuteAsync(OperationsCurrentUser.Context(http), new ScanIntoOperationsBundleRequest
            {
                BundleId = bundleId,
                Code = code,
                ScanKey = Guid.TryParseExact(key, "N", out Guid scanKey) ? scanKey : Guid.Empty
            }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return await FillAsync(http, bundleId, null, response.Error, null, get, brand, errors);
            }

            string note = response.Repeat ? "repeat" : string.Join('.', new[] { response.PastTarget ? "past" : null, response.Split ? "split" : null, "added" }.OfType<string>());
            return Results.Redirect("/staff/bundles/" + bundleId + "/fill?note=" + note + (response.ParcelId is { } parcelId ? "&parcel=" + parcelId : string.Empty));
        });

        staff.MapPost("/bundles/{bundleId:guid}/take-out", async (HttpContext http, Guid bundleId, [FromForm] Guid? parcel, [FromForm] Guid? box, TakeOutOfOperationsBundleUseCase takeOut,
            GetOperationsBundleUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            TakeOutOfOperationsBundleResponse response = await takeOut.ExecuteAsync(OperationsCurrentUser.Context(http),
                new TakeOutOfOperationsBundleRequest { BundleId = bundleId, ParcelId = parcel, BoxId = box }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bundles/" + bundleId + "/fill?note=out") : await FillAsync(http, bundleId, null, response.Error, null, get, brand, errors);
        });

        MapBoxes(staff);
        MapSettings(staff);
        return endpoints;
    }

    private static void MapBoxes(RouteGroupBuilder staff)
    {
        staff.MapGet("/bundles/boxes", async (HttpContext http, GetOperationsRepackBoxesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await BoxesAsync(http, null, get, brand, errors));

        staff.MapPost("/bundles/boxes/way", async (HttpContext http, [FromForm] Guid? shipment, [FromForm] string? way, ChooseOperationsConsolidationUseCase choose,
            GetOperationsRepackBoxesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ChooseOperationsConsolidationResponse response = await choose.ExecuteAsync(OperationsCurrentUser.Context(http), new ChooseOperationsConsolidationRequest
            {
                ShipmentId = shipment ?? Guid.Empty,
                Way = Enum.TryParse(way, false, out OperationsConsolidationWay parsed) ? parsed : 0
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bundles/boxes") : await BoxesAsync(http, response.Error, get, brand, errors);
        });

        staff.MapPost("/bundles/boxes", async (HttpContext http, [FromForm] Guid? shipment, [FromForm] string? key, OpenOperationsRepackBoxUseCase open, GetOperationsRepackBoxesUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OpenOperationsRepackBoxResponse response = await open.ExecuteAsync(OperationsCurrentUser.Context(http), new OpenOperationsRepackBoxRequest
            {
                ShipmentId = shipment ?? Guid.Empty,
                RequestKey = Guid.TryParseExact(key, "N", out Guid formKey) ? formKey : null
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bundles/boxes/" + response.BoxId) : await BoxesAsync(http, response.Error, get, brand, errors);
        });

        staff.MapGet("/bundles/boxes/{boxId:guid}", async (HttpContext http, Guid boxId, string? note, GetOperationsRepackBoxUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
            await BoxAsync(http, boxId, null, Note(texts, note), get, brand, errors));

        staff.MapPost("/bundles/boxes/{boxId:guid}/scan", async (HttpContext http, Guid boxId, [FromForm] string? code, [FromForm] string? key, ScanIntoOperationsRepackBoxUseCase scan,
            GetOperationsRepackBoxUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ScanIntoOperationsRepackBoxResponse response = await scan.ExecuteAsync(OperationsCurrentUser.Context(http), new ScanIntoOperationsRepackBoxRequest
            {
                BoxId = boxId,
                Code = code,
                ScanKey = Guid.TryParseExact(key, "N", out Guid scanKey) ? scanKey : Guid.Empty
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/bundles/boxes/" + boxId + "?note=" + (response.Repeat ? "repeat" : "added"))
                : await BoxAsync(http, boxId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/bundles/boxes/{boxId:guid}/take-out", async (HttpContext http, Guid boxId, [FromForm] Guid? parcel, TakeOutOfOperationsRepackBoxUseCase takeOut,
            GetOperationsRepackBoxUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            TakeOutOfOperationsRepackBoxResponse response = await takeOut.ExecuteAsync(OperationsCurrentUser.Context(http),
                new TakeOutOfOperationsRepackBoxRequest { BoxId = boxId, ParcelId = parcel ?? Guid.Empty }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bundles/boxes/" + boxId + "?note=out") : await BoxAsync(http, boxId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/bundles/boxes/{boxId:guid}/close", async (HttpContext http, Guid boxId, [FromForm] string? weight, [FromForm] string? length, [FromForm] string? width,
            [FromForm] string? height, CloseOperationsRepackBoxUseCase close, GetOperationsRepackBoxUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            CloseOperationsRepackBoxResponse response = await close.ExecuteAsync(OperationsCurrentUser.Context(http), new CloseOperationsRepackBoxRequest
            {
                BoxId = boxId,
                Weight = Size(weight),
                Length = Size(length),
                Width = Size(width),
                Height = Size(height)
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bundles/boxes/" + boxId + "?note=closed") : await BoxAsync(http, boxId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/bundles/boxes/{boxId:guid}/void", async (HttpContext http, Guid boxId, [FromForm] string? reason, VoidOperationsRepackBoxUseCase voiding,
            GetOperationsRepackBoxUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            VoidOperationsRepackBoxResponse response = await voiding.ExecuteAsync(OperationsCurrentUser.Context(http), new VoidOperationsRepackBoxRequest { BoxId = boxId, Reason = reason },
                http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bundles/boxes") : await BoxAsync(http, boxId, response.Error, null, get, brand, errors);
        });

        staff.MapGet("/bundles/boxes/{boxId:guid}/label", async (HttpContext http, Guid boxId, GetOperationsRepackBoxLabelUseCase label) =>
        {
            GetOperationsRepackBoxLabelResponse response = await label.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsRepackBoxLabelRequest { BoxId = boxId },
                http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Pdf, "application/pdf", null) : Results.NotFound();
        });
    }

    private static void MapSettings(RouteGroupBuilder staff)
    {
        staff.MapGet("/settings/bundles", async (HttpContext http, string? done, GetOperationsBundleSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> texts) =>
            await SettingsAsync(http, null, done is null ? null : texts["ui.staff.done.saved"].Value, get, brand, errors));

        staff.MapPost("/settings/bundles", async (HttpContext http, [FromForm] string? types, [FromForm] string? rule, [FromForm] string? way, SaveOperationsBundleSettingsUseCase save,
            GetOperationsBundleSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            SaveOperationsBundleSettingsResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsBundleSettingsRequest
            {
                Types = (types ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(OperationsBundleLimits.MaxTypes + 1).ToList(),
                TargetRule = Enum.TryParse(rule, false, out OperationsBundleTargetRule target) ? target : 0,
                Way = Enum.TryParse(way, false, out OperationsConsolidationWay consolidation) ? consolidation : 0
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/bundles?done=1") : await SettingsAsync(http, response.Error, null, get, brand, errors);
        });
    }

    // A weight or a side as typed, in the company's units; unreadable: zero, which the domain refuses.
    private static decimal Size(string? typed) => OperationsCounterEndpoints.Amount(typed) ?? 0m;

    // What the last scan did, beside the station: added, a repeat, past the target, a split.
    private static string? Note(IStringLocalizer<OperationsText> texts, string? note) =>
        note is null ? null : string.Join(" ", note.Split('.').Where(part => part is "added" or "repeat" or "past" or "split" or "out" or "closed").Distinct().Select(part => texts["ui.bundles.note." + part].Value));

    private static async Task<IResult> DeskAsync(HttpContext http, Error? error, string? info, GetOperationsBundlesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsBundlesResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsBundlesRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<BundlesPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Warehouses,
            response.AllWarehouses,
            response.Ready,
            response.Bundles,
            response.Services,
            response.Types,
            response.ManageWarehouses,
            response.Divisors,
            response.WeightUnit,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> BundleAsync(HttpContext http, Guid bundleId, Error? error, string? info, GetOperationsBundleUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsBundleResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsBundleRequest { BundleId = bundleId }, http.RequestAborted);

        if (!response.Succeeded)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<BundlePage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Bundle,
            response.Warehouse,
            response.Destination,
            response.Parcels,
            response.Shipments,
            response.CanManage,
            response.CanFill,
            response.WeightUnit,
            response.DimensionUnit,
            response.List,
            response.Candidates,
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static async Task<IResult> FillAsync(HttpContext http, Guid bundleId, Guid? parcelId, Error? error, string? info, GetOperationsBundleUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsBundleResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsBundleRequest { BundleId = bundleId, FocusParcelId = parcelId },
            http.RequestAborted);

        if (!response.Succeeded)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<FillBundlePage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Bundle,
            response.Parcels,
            response.Focus,
            response.WeightUnit,
            response.List,
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static async Task<IResult> BoxesAsync(HttpContext http, Error? error, GetOperationsRepackBoxesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsRepackBoxesResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsRepackBoxesRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<RepackBoxesPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Shipments,
            response.Warehouses,
            response.ManageWarehouses,
            response.CompanyWay,
            response.FillWarehouses,
            Error = errors.Describe(error ?? response.Error)
        });
    }

    private static async Task<IResult> BoxAsync(HttpContext http, Guid boxId, Error? error, string? info, GetOperationsRepackBoxUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsRepackBoxResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsRepackBoxRequest { BoxId = boxId }, http.RequestAborted);

        if (!response.Succeeded)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<RepackBoxPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Box,
            response.Shipment,
            response.Warehouse,
            response.Parcels,
            response.Shelves,
            response.BundleNumber,
            response.CanManage,
            response.CanFill,
            response.WeightUnit,
            response.DimensionUnit,
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static async Task<IResult> SettingsAsync(HttpContext http, Error? error, string? info, GetOperationsBundleSettingsUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsBundleSettingsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsBundleSettingsRequest(), http.RequestAborted);

        if (!response.Succeeded)
        {
            return Results.Forbid();
        }

        return OperationsStaffResults.Page<BundleSettingsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Types,
            response.TargetRule,
            response.Way,
            response.CanEdit,
            Error = errors.Describe(error),
            Info = info
        });
    }
}
