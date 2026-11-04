using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.UseCases.Receiving.CloseOperationsDelivery;
using Vixly.Operations.Application.UseCases.Receiving.GetOperationsReceiptDocument;
using Vixly.Operations.Application.UseCases.Receiving.GetOperationsScanStation;
using Vixly.Operations.Application.UseCases.Receiving.ListOperationsReceipts;
using Vixly.Operations.Application.UseCases.Receiving.OpenOperationsDelivery;
using Vixly.Operations.Application.UseCases.Receiving.ScanOperationsParcel;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Parcels;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Hosting;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsReceivingEndpoints.cs
//
// PURPOSE:
// The dock: the scan app, its calls, and the carrier deliveries with their receipts (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - /staff/scan: the scan app (fixed station, handheld or phone camera)
// - /staff/scan/api/*: station, open a delivery, scan, close
// - /staff/receiving: open deliveries and receipts; /staff/receipts/{id}: the receipt PDF
//
// IMPORTANT NOTES:
// - Calls carry the antiforgery token in a header; each scan carries its own reference, so a scan sent twice (offline queue, retry) counts once
// - Only the scan app may use the camera (Permissions-Policy camera=(self))
// =========================================================
public static class OperationsReceivingEndpoints
{
    // The scan app may use the device camera; every other page keeps it off (VixlySecurityHeaders).
    private const string CameraPolicy = "camera=(self), microphone=(), geolocation=(), payment=()";

    public static IEndpointRouteBuilder MapOperationsReceivingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/scan", async (HttpContext http, Guid? warehouse, GetOperationsScanStationUseCase station, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            GetOperationsScanStationResponse response = await station.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsScanStationRequest { WarehouseId = warehouse }, http.RequestAborted);
            http.Items[VixlySecurityHeaders.PermissionsPolicyItem] = CameraPolicy;

            return OperationsStaffResults.Page<ScanPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                Station = response,
                Error = errors.Describe(response.Error)
            });
        });

        RouteGroupBuilder api = staff.MapGroup("/scan/api");

        api.MapPost("/deliveries", async (HttpContext http, [FromBody] ScanOpenBody body, OpenOperationsDeliveryUseCase open, OperationsErrorText errors) =>
        {
            OpenOperationsDeliveryResponse response = await open.ExecuteAsync(OperationsCurrentUser.Context(http),
                new OpenOperationsDeliveryRequest { WarehouseId = body.WarehouseId, Carrier = body.Carrier, CommandKey = body.Key ?? string.Empty }, http.RequestAborted);
            return response.Succeeded ? Results.Ok(new { deliveryId = response.DeliveryId }) : Refused(errors, response.Error);
        });

        api.MapPost("/deliveries/{deliveryId:guid}/scans", async (HttpContext http, Guid deliveryId, [FromBody] ScanBody body, ScanOperationsParcelUseCase scan,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
        {
            ScanOperationsParcelResponse response = await scan.ExecuteAsync(OperationsCurrentUser.Context(http), new ScanOperationsParcelRequest
            {
                DeliveryId = deliveryId,
                Barcode = body.Barcode ?? string.Empty,
                ScanKey = body.Key ?? string.Empty,
                Damaged = body.Damaged,
                PrinterId = body.PrinterId,
                PortalSlug = OperationsStaffResults.PortalSlug(http)
            }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Refused(errors, response.Error);
            }

            return Results.Ok(new
            {
                parcelId = response.ParcelId,
                number = response.Number,
                outcome = response.Outcome,
                outcomeText = text["ui.scan.outcome." + response.Outcome].Value,
                customerCode = response.CustomerCode,
                customerName = response.CustomerName,
                queued = response.Queued,
                barcode = OperationsLabelSvg.Barcode(response.Number)
            });
        });

        api.MapPost("/deliveries/{deliveryId:guid}/close", async (HttpContext http, Guid deliveryId, CloseOperationsDeliveryUseCase close, OperationsErrorText errors) =>
        {
            CloseOperationsDeliveryResponse response = await close.ExecuteAsync(OperationsCurrentUser.Context(http), new CloseOperationsDeliveryRequest { DeliveryId = deliveryId }, http.RequestAborted);
            return response.Succeeded
                ? Results.Ok(new { receiptId = response.ReceiptId, number = response.Number, url = "/staff/receipts/" + response.ReceiptId })
                : Refused(errors, response.Error);
        });

        staff.MapGet("/receiving", async (HttpContext http, int? page, ListOperationsReceiptsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ListOperationsReceiptsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsReceiptsRequest { Page = Math.Max(page ?? 1, 1) }, http.RequestAborted);

            return OperationsStaffResults.Page<ReceivingPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                Open = response.Open,
                Receipts = response.Receipts,
                TotalCount = response.TotalCount,
                Warehouses = response.Warehouses,
                Page = Math.Max(page ?? 1, 1),
                Error = errors.Describe(response.Error)
            });
        });

        staff.MapGet("/receipts/{receiptId:guid}", async (HttpContext http, Guid receiptId, GetOperationsReceiptDocumentUseCase document) =>
        {
            GetOperationsReceiptDocumentResponse response = await document.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsReceiptDocumentRequest { ReceiptId = receiptId }, http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Content, "application/pdf", response.FileName) : Results.NotFound();
        });

        return endpoints;
    }

    private static IResult Refused(OperationsErrorText errors, Error? error) =>
        Results.Json(new { code = error?.Code, message = errors.Describe(error) }, statusCode: error?.Code == OperationsAccessErrors.SignInRequired.Code ? StatusCodes.Status401Unauthorized : StatusCodes.Status400BadRequest);
}
