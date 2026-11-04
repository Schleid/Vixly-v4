using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.UseCases.Receiving.CheckOperationsParcel;
using Vixly.Operations.Application.UseCases.Receiving.FindOperationsParcelByScan;
using Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcel;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Parcels;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsInspectionEndpoints.cs
//
// PURPOSE:
// The inspection station: scan a parcel, record its condition, weight, size and photos (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - /staff/inspection: scan; /staff/inspection/{id}: the parcel with its customer and shipment, and the check form
//
// IMPORTANT NOTES:
// - A scanner types the ID label and presses Enter: the form needs no click
// - Static forms with antiforgery and the form guard; each check carries its own reference (ADR-0019)
// =========================================================
public static class OperationsInspectionEndpoints
{
    // Six phone photos of up to 10 MB each, and the form fields.
    private const long UploadLimit = (OperationsReceivingLimits.MaxPhotosPerReceipt * (long)OperationsReceivingLimits.MaxPhotoBytes) + (1024 * 1024);

    private static readonly string[] PhotoFields = { "package", "label", "damage", "contents" };

    public static IEndpointRouteBuilder MapOperationsInspectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/inspection", async (HttpContext http, string? code, string? done, string? last, FindOperationsParcelByScanUseCase find, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
        {
            Error? error = null;

            if (!string.IsNullOrWhiteSpace(code))
            {
                FindOperationsParcelByScanResponse found = await find.ExecuteAsync(OperationsCurrentUser.Context(http), new FindOperationsParcelByScanRequest { Barcode = code }, http.RequestAborted);

                if (found.Succeeded)
                {
                    return Results.Redirect("/staff/inspection/" + found.ParcelId);
                }

                error = found.Error;
            }

            string? info = done is "checked" or "selected" or "labelled" ? VixlyText.Format(text, "ui.inspection.done." + done, "number", Clean(last)) : null;
            return OperationsStaffResults.Page<InspectionPage>(http, new { Company = await OperationsStaffResults.CompanyAsync(http, brand), Error = errors.Describe(error), Info = info });
        });

        staff.MapGet("/inspection/{parcelId:guid}", async (HttpContext http, Guid parcelId, GetOperationsParcelUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await StationAsync(http, parcelId, null, get, brand, errors));

        staff.MapPost("/inspection/{parcelId:guid}", async (HttpContext http, Guid parcelId, CheckOperationsParcelUseCase check, GetOperationsParcelUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            List<OperationsParcelPhotoUpload> photos = new();

            foreach (string kind in PhotoFields)
            {
                foreach (IFormFile file in form.Files.GetFiles("photo_" + kind).Where(file => file.Length > 0).Take(OperationsReceivingLimits.MaxPhotosPerReceipt))
                {
                    if (file.Length > OperationsReceivingLimits.MaxPhotoBytes)
                    {
                        return await StationAsync(http, parcelId, OperationsReceivingErrors.PhotoInvalid, get, brand, errors);
                    }

                    using MemoryStream buffer = new((int)file.Length);
                    await file.CopyToAsync(buffer, http.RequestAborted);
                    photos.Add(new OperationsParcelPhotoUpload(kind, buffer.ToArray()));
                }
            }

            CheckOperationsParcelResponse response = await check.ExecuteAsync(OperationsCurrentUser.Context(http), new CheckOperationsParcelRequest
            {
                ParcelId = parcelId,
                Condition = form["condition"].ToString(),
                Weight = Number(form["weight"]),
                Length = Number(form["length"]),
                Width = Number(form["width"]),
                Height = Number(form["height"]),
                Photos = photos,
                CommandKey = form["key"].ToString(),
                Flag = form["flag"].ToString() == "true",
                FlagReason = form["flagReason"].ToString(),
                ShippingPrinterId = Guid.TryParse(form["shippingPrinter"], out Guid shippingPrinter) ? shippingPrinter : null
            }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return await StationAsync(http, parcelId, response.Error, get, brand, errors);
            }

            GetOperationsParcelResponse parcel = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsParcelRequest { ParcelId = parcelId }, http.RequestAborted);
            string done = response.SelectedForInspection ? "selected" : response.ShippingLabelPrinted ? "labelled" : "checked";
            return Results.Redirect("/staff/inspection?done=" + done + "&last=" + Uri.EscapeDataString(parcel.Parcel?.Number ?? string.Empty));
        }).WithMetadata(new OperationsUploadLimit(UploadLimit)).WithFormOptions(multipartBodyLengthLimit: UploadLimit);

        return endpoints;
    }

    private static async Task<IResult> StationAsync(HttpContext http, Guid parcelId, Error? error, GetOperationsParcelUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsParcelResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsParcelRequest { ParcelId = parcelId }, http.RequestAborted);

        if (response.Parcel is not { } parcel)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<InspectionPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Parcel = response.CanCheck ? parcel : null,
            CustomerPhone = response.CustomerPhone,
            ShipmentParcels = response.ShipmentParcels,
            Shipment = response.Shipment,
            Condition = response.Condition,
            WeightUnit = response.WeightUnit,
            DimensionUnit = response.DimensionUnit,
            CommandKey = Guid.NewGuid().ToString("N"),
            ShippingPrinters = response.Printers.Where(printer => printer.Media == Vixly.Operations.Domain.Printing.OperationsLabelMedia.ShippingLabel).ToList(),
            Error = errors.Describe(error ?? (response.CanCheck ? null : OperationsAccessErrors.PermissionRequired))
        });
    }

    // Browsers send number fields with a dot; anything with a group separator is refused rather than guessed (review M3).
    private static decimal Number(string? value) =>
        value is { Length: > 0 and <= 20 } && value.Trim().All(character => char.IsAsciiDigit(character) || character == '.')
        && decimal.TryParse(value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number)
            ? number
            : 0m;

    private static string Clean(string? text) => new((text ?? string.Empty).Where(char.IsAsciiLetterOrDigit).Take(24).ToArray());
}
