using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.UseCases.Printing.PrintOperationsParcelLabel;
using Vixly.Operations.Application.UseCases.Receiving.AssignOperationsParcel;
using Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcel;
using Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcelLabel;
using Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcelPhoto;
using Vixly.Operations.Application.UseCases.Receiving.ListOperationsParcels;
using Vixly.Operations.Application.UseCases.Receiving.ListOperationsStorageLocations;
using Vixly.Operations.Application.UseCases.Receiving.PutAwayOperationsParcel;
using Vixly.Operations.Application.UseCases.Receiving.SaveOperationsStorageLocation;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Parcels;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Operations.Web.Tenancy;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsParcelEndpoints.cs
//
// PURPOSE:
// The staff receiving, parcel and shelf pages (Slice 3B, design section 8).
//
// RESPONSIBILITIES:
// - /staff/parcels, /staff/parcels/unidentified and /staff/parcels/{id}: find, assign, put away, print the label
// - /staff/locations/warehouse/{id}/storage: the shelves of a warehouse
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard; each command carries a reference so a double click changes nothing twice (ADR-0019)
// - The use cases check permissions and the warehouse scope; pages only show what they return
// - Photos and labels are sent with no-store and nosniff, never cached by a shared proxy
// =========================================================
public static class OperationsParcelEndpoints
{
    public static IEndpointRouteBuilder MapOperationsParcelEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/parcels", async (HttpContext http, string? search, Guid? warehouse, bool? held, int? page, ListOperationsParcelsUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await ListAsync(http, search, warehouse, false, held == true, Math.Max(page ?? 1, 1), list, brand, errors));

        staff.MapGet("/parcels/unidentified", async (HttpContext http, string? search, Guid? warehouse, int? page, ListOperationsParcelsUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await ListAsync(http, search, warehouse, true, false, Math.Max(page ?? 1, 1), list, brand, errors));

        staff.MapGet("/parcels/{parcelId:guid}", async (HttpContext http, Guid parcelId, string? done, string? error, GetOperationsParcelUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await ParcelAsync(http, parcelId, OperationsShippingLabelEndpoints.Known(error),
                done is "assigned" or "put_away" or "printed" or "checked" or "selected" ? text["ui.parcels.done." + done].Value
                : done is "label_printed" or "label_recorded" ? text[done == "label_printed" ? "ui.shipping_label.done.label_printed" : "ui.undeclared.done.label_recorded"].Value : null,
                get, brand, errors));

        staff.MapGet("/parcels/{parcelId:guid}/photos/{photoId:guid}", async (HttpContext http, Guid parcelId, Guid photoId, GetOperationsParcelPhotoUseCase photo) =>
        {
            GetOperationsParcelPhotoResponse response = await photo.ExecuteAsync(OperationsCurrentUser.Context(http),
                new GetOperationsParcelPhotoRequest { ParcelId = parcelId, PhotoId = photoId }, http.RequestAborted);
            return response.Succeeded ? File(http, response.Content, response.ContentType, null) : Results.NotFound();
        });

        staff.MapGet("/parcels/{parcelId:guid}/label", async (HttpContext http, Guid parcelId, GetOperationsParcelLabelUseCase label) =>
        {
            GetOperationsParcelLabelResponse response = await label.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsParcelLabelRequest { ParcelId = parcelId }, http.RequestAborted);
            return response.Succeeded ? File(http, response.Content, "application/pdf", response.FileName) : Results.NotFound();
        });

        staff.MapPost("/parcels/{parcelId:guid}/assign", async (HttpContext http, Guid parcelId, [FromForm] string? customer, [FromForm] string? verifiedBy, [FromForm] string? key,
            AssignOperationsParcelUseCase assign, GetOperationsParcelUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            AssignOperationsParcelResponse response = await assign.ExecuteAsync(OperationsCurrentUser.Context(http), new AssignOperationsParcelRequest
            {
                ParcelId = parcelId,
                CustomerCode = customer ?? string.Empty,
                VerifiedBy = verifiedBy ?? string.Empty,
                CommandKey = key ?? string.Empty,
                PortalSlug = PortalSlug(http)
            }, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/parcels/" + parcelId + "?done=assigned")
                : await ParcelAsync(http, parcelId, response.Error, null, get, brand, errors);
        });

        // Slice 4D step 3: what the label of an undeclared package shows.
        staff.MapPost("/parcels/{parcelId:guid}/label-text", async (HttpContext http, Guid parcelId, [FromForm] string? name, [FromForm] string? code, [FromForm] string? sender,
            Vixly.Operations.Application.UseCases.Receiving.RecordOperationsParcelLabel.RecordOperationsParcelLabelUseCase record, GetOperationsParcelUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Vixly.Operations.Application.UseCases.Receiving.RecordOperationsParcelLabel.RecordOperationsParcelLabelResponse response = await record.ExecuteAsync(OperationsCurrentUser.Context(http),
                new Vixly.Operations.Application.UseCases.Receiving.RecordOperationsParcelLabel.RecordOperationsParcelLabelRequest { ParcelId = parcelId, Name = name, Code = code, Sender = sender },
                http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/parcels/" + parcelId + "?done=label_recorded") : await ParcelAsync(http, parcelId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/parcels/{parcelId:guid}/print", async (HttpContext http, Guid parcelId, [FromForm] Guid? printer, PrintOperationsParcelLabelUseCase print,
            GetOperationsParcelUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            PrintOperationsParcelLabelResponse response = await print.ExecuteAsync(OperationsCurrentUser.Context(http),
                new PrintOperationsParcelLabelRequest { PrinterId = printer ?? Guid.Empty, ParcelId = parcelId }, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/parcels/" + parcelId + "?done=printed")
                : await ParcelAsync(http, parcelId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/parcels/{parcelId:guid}/put-away", async (HttpContext http, Guid parcelId, [FromForm] Guid? location, [FromForm] string? key,
            PutAwayOperationsParcelUseCase putAway, GetOperationsParcelUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            PutAwayOperationsParcelResponse response = await putAway.ExecuteAsync(OperationsCurrentUser.Context(http), new PutAwayOperationsParcelRequest
            {
                ParcelId = parcelId,
                StorageLocationId = location ?? Guid.Empty,
                CommandKey = key ?? string.Empty
            }, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/parcels/" + parcelId + "?done=put_away")
                : await ParcelAsync(http, parcelId, response.Error, null, get, brand, errors);
        });

        staff.MapGet("/locations/warehouse/{warehouseId:guid}/storage", async (HttpContext http, Guid warehouseId, string? done, ListOperationsStorageLocationsUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await ShelvesAsync(http, warehouseId, null, done == "saved" ? text["ui.storage.saved"].Value : null, list, brand, errors));

        staff.MapPost("/locations/warehouse/{warehouseId:guid}/storage", async (HttpContext http, Guid warehouseId, [FromForm] Guid? location, [FromForm] string? code,
            [FromForm] string? zone, [FromForm] string? active, SaveOperationsStorageLocationUseCase save, ListOperationsStorageLocationsUseCase list, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            if (!Enum.TryParse(zone, ignoreCase: true, out OperationsStorageZone chosen) || !Enum.IsDefined(chosen) || int.TryParse(zone, out _))
            {
                return await ShelvesAsync(http, warehouseId, OperationsReceivingErrors.LocationCodeInvalid, null, list, brand, errors);
            }

            SaveOperationsStorageLocationResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsStorageLocationRequest
            {
                WarehouseId = warehouseId,
                LocationId = location,
                Code = code,
                Zone = chosen,
                Active = location is null || active == "true"
            }, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/locations/warehouse/" + warehouseId + "/storage?done=saved")
                : await ShelvesAsync(http, warehouseId, response.Error, null, list, brand, errors);
        });

        return endpoints;
    }

    // Browsers send number fields with a dot; anything with a group separator is refused rather than guessed (review M3).
    private static decimal Number(string? value) =>
        value is { Length: > 0 and <= 20 } && value.Trim().All(character => char.IsAsciiDigit(character) || character == '.')
        && decimal.TryParse(value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number)
            ? number
            : 0m;

    private static string PortalSlug(HttpContext http) => OperationsTenantMiddleware.Host(http)?.CanonicalSlug ?? string.Empty;

    private static IResult File(HttpContext http, byte[] content, string contentType, string? fileName)
    {
        http.Response.Headers.CacheControl = "private, no-store";
        http.Response.Headers.XContentTypeOptions = "nosniff";
        http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; img-src 'self'; sandbox";

        if (fileName is not null)
        {
            http.Response.Headers.ContentDisposition = "inline; filename=\"" + fileName + "\"";
        }

        return Results.Bytes(content, contentType);
    }

    private static async Task<IResult> ListAsync(HttpContext http, string? search, Guid? warehouse, bool unidentified, bool held, int page, ListOperationsParcelsUseCase list,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        ListOperationsParcelsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsParcelsRequest
        {
            Search = search,
            WarehouseId = warehouse,
            UnidentifiedOnly = unidentified,
            HeldOnly = held,
            Page = page
        }, http.RequestAborted);

        return Page<ParcelListPage>(http, new
        {
            Company = await CompanyAsync(http, brand),
            Search = search,
            WarehouseId = warehouse,
            Unidentified = unidentified,
            Held = held,
            Page = page,
            Parcels = response.Parcels,
            TotalCount = response.TotalCount,
            Warehouses = response.Warehouses,
            WeightUnit = response.WeightUnit,
            Error = errors.Describe(response.Error)
        });
    }

    private static async Task<IResult> ParcelAsync(HttpContext http, Guid parcelId, Error? error, string? info, GetOperationsParcelUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsParcelResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsParcelRequest { ParcelId = parcelId }, http.RequestAborted);

        if (response.Parcel is not { } parcel)
        {
            return Results.NotFound();
        }

        return Page<ParcelPage>(http, new
        {
            Company = await CompanyAsync(http, brand),
            Parcel = parcel,
            History = response.History,
            Photos = response.Photos,
            ReceiptNumber = response.ReceiptNumber,
            Condition = response.Condition,
            Locations = response.Locations,
            CanAssign = response.CanAssign,
            CanPutAway = response.CanPutAway,
            CanCheck = response.CanCheck,
            CustomerPhone = response.CustomerPhone,
            ShipmentParcels = response.ShipmentParcels,
            Shipment = response.Shipment,
            Inspection = response.Inspection,
            Printers = response.Printers,
            ShippingCleared = response.ShippingCleared,
            LabelCustomer = response.LabelCustomer,
            LabelCustomerNamed = response.LabelCustomerNamed,
            StorageFee = response.StorageFee,
            StorageDays = response.StorageDays,
            CurrencyCode = response.CurrencyCode,
            WeightUnit = response.WeightUnit,
            DimensionUnit = response.DimensionUnit,
            CommandKey = Guid.NewGuid().ToString("N"),
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static async Task<IResult> ShelvesAsync(HttpContext http, Guid warehouseId, Error? error, string? info, ListOperationsStorageLocationsUseCase list,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        ListOperationsStorageLocationsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http),
            new ListOperationsStorageLocationsRequest { WarehouseId = warehouseId }, http.RequestAborted);

        if (response.Warehouse is not { } warehouse)
        {
            return Results.NotFound();
        }

        return Page<StorageLocationsPage>(http, new
        {
            Company = await CompanyAsync(http, brand),
            Warehouse = warehouse,
            Locations = response.Locations,
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static IResult Page<TComponent>(HttpContext http, object parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        http.Response.Headers.CacheControl = "no-store";
        return new RazorComponentResult<TComponent>(parameters);
    }

    private static async Task<string?> CompanyAsync(HttpContext http, GetOperationsSignInBrandUseCase brand) =>
        (await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted)).DisplayName;
}
