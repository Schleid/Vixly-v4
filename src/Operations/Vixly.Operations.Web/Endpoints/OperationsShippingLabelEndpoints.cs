using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Printing.GetOperationsShippingLabel;
using Vixly.Operations.Application.UseCases.Printing.ListOperationsLabelsToPrint;
using Vixly.Operations.Application.UseCases.Printing.PrintOperationsShippingLabel;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Parcels;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsShippingLabelEndpoints.cs
//
// PURPOSE:
// Shipping labels for staff: print or download a parcel's label, and the labels to print of a warehouse (Slice 4D step 2, A-5).
//
// RESPONSIBILITIES:
// - /staff/parcels/{id}/shipping-label: print on a printer holding shipping labels; /shipping-label/pdf: the PDF (marks it printed)
// - /staff/parcels/labels: cleared parcels whose label was never printed; print one or all of them
//
// IMPORTANT NOTES:
// - Forms with antiforgery and the form guard (ADR-0019); the use cases check the permission and the warehouse
// - The PDF is sent with no-store and nosniff
// =========================================================
public static class OperationsShippingLabelEndpoints
{
    public static IEndpointRouteBuilder MapOperationsShippingLabelEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff/parcels").RequireAuthorization().RequireFormToken();

        staff.MapPost("/{parcelId:guid}/shipping-label", async (HttpContext http, Guid parcelId, [FromForm] Guid? printer, [FromForm] string? back, [FromForm] Guid? warehouse,
            PrintOperationsShippingLabelUseCase print) =>
        {
            PrintOperationsShippingLabelResponse response = await print.ExecuteAsync(OperationsCurrentUser.Context(http),
                new PrintOperationsShippingLabelRequest { ParcelId = parcelId, PrinterId = printer ?? Guid.Empty }, http.RequestAborted);
            return Results.Redirect(Back(parcelId, back, warehouse) + (response.Succeeded ? "done=label_printed" : "error=" + Uri.EscapeDataString(response.Error!.Code)));
        });

        staff.MapPost("/{parcelId:guid}/shipping-label/pdf", async (HttpContext http, Guid parcelId, [FromForm] string? back, [FromForm] Guid? warehouse, GetOperationsShippingLabelUseCase get) =>
        {
            GetOperationsShippingLabelResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsShippingLabelRequest { ParcelId = parcelId }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.Redirect(Back(parcelId, back, warehouse) + "error=" + Uri.EscapeDataString(response.Error!.Code));
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(response.Content, "application/pdf", response.FileName);
        });

        staff.MapGet("/labels", async (HttpContext http, Guid? warehouse, string? done, int? count, string? error, ListOperationsLabelsToPrintUseCase list, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
        {
            ListOperationsLabelsToPrintResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsLabelsToPrintRequest { WarehouseId = warehouse },
                http.RequestAborted);
            return OperationsStaffResults.Page<LabelsToPrintPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),

                // A company with one warehouse prints all of its labels at once without choosing it (review of 4D step 2).
                WarehouseId = warehouse ?? (response.Warehouses.Count == 1 ? response.Warehouses[0].Id : null),
                Parcels = response.Parcels,
                Printers = response.Printers,
                Warehouses = response.Warehouses,
                Info = done == "label_printed" ? text["ui.shipping_label.done.label_printed"].Value
                    : done == "all_printed" ? Vixly.Shared.Localization.VixlyText.Format(text, "ui.shipping_label.done.all_printed", "count", Math.Clamp(count ?? 0, 0, MaxAtOnce)) : null,
                Error = errors.Describe(response.Error ?? Known(error))
            });
        });

        // Every label on the list of one warehouse, on one printer; one that is no longer cleared is skipped.
        staff.MapPost("/labels/print", async (HttpContext http, [FromForm] Guid? warehouse, [FromForm] Guid? printer, ListOperationsLabelsToPrintUseCase list,
            PrintOperationsShippingLabelUseCase print) =>
        {
            ListOperationsLabelsToPrintResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsLabelsToPrintRequest { WarehouseId = warehouse },
                http.RequestAborted);
            Error? failed = response.Error;
            int sent = 0;

            // A parcel printed meanwhile by another clerk is skipped; what remains stays on the list (review of 4D step 2).
            foreach (Vixly.Operations.Application.Receiving.OperationsParcelSummary parcel in response.Parcels.Take(MaxAtOnce))
            {
                PrintOperationsShippingLabelResponse printed = await print.ExecuteAsync(OperationsCurrentUser.Context(http),
                    new PrintOperationsShippingLabelRequest { ParcelId = parcel.ParcelId, PrinterId = printer ?? Guid.Empty, OnlyIfNotPrinted = true }, http.RequestAborted);

                if (!printed.Succeeded && printed.Error!.Code != "ops.printing.not_cleared")
                {
                    failed = printed.Error;
                    break;
                }

                sent += printed.Succeeded && !printed.Skipped ? 1 : 0;
            }

            return Results.Redirect("/staff/parcels/labels" + (warehouse is { } chosen ? "?warehouse=" + chosen + "&" : "?")
                + (failed is null ? "done=all_printed&count=" + sent : "error=" + Uri.EscapeDataString(failed.Code)));
        });

        return endpoints;
    }

    public const int MaxAtOnce = 50;

    // Back to the labels to print (with its warehouse) or to the parcel's page, never to an address from the form.
    private static string Back(Guid parcelId, string? back, Guid? warehouse) =>
        back == "labels" ? "/staff/parcels/labels" + (warehouse is { } chosen ? "?warehouse=" + chosen + "&" : "?") : "/staff/parcels/" + parcelId + "?";

    // Only codes of these pages come back in the address, so the page never shows an arbitrary text.
    internal static Error? Known(string? code) => code switch
    {
        "ops.printing.not_cleared" => Vixly.Operations.Domain.Printing.OperationsPrintingErrors.NotCleared,
        "ops.printing.printer_not_found" => Vixly.Operations.Domain.Printing.OperationsPrintingErrors.PrinterNotFound,
        "ops.receiving.parcel_not_found" => Vixly.Operations.Domain.Receiving.OperationsReceivingErrors.ParcelNotFound,
        "ops.common.concurrency_conflict" => Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict,
        _ => null
    };
}
