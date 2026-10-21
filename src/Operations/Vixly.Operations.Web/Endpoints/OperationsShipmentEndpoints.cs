using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Application.UseCases.Shipments.ChangeOperationsShipmentService;
using Vixly.Operations.Application.UseCases.Shipments.GetOperationsShipment;
using Vixly.Operations.Application.UseCases.Shipments.GetOperationsShipmentDocument;
using Vixly.Operations.Application.UseCases.Shipments.ListOperationsShipments;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Shipments;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsShipmentEndpoints.cs
//
// PURPOSE:
// The shipments desk and the shipment documents for staff (Slice 5A).
//
// RESPONSIBILITIES:
// - /staff/shipments: the shipments of the user's warehouses, with views, filters and the CSV export
// - /staff/shipments/{id}: one shipment; change its service
// - /staff/shipments/{id}/documents/{kind}: the packing list and the pro forma commercial invoice (PDF)
//
// IMPORTANT NOTES:
// - Forms with antiforgery and the form guard (ADR-0019); the use cases check the permission and the warehouse
// - The export is formula-safe, as the incoming list
// =========================================================
public static class OperationsShipmentEndpoints
{
    public static IEndpointRouteBuilder MapOperationsShipmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff/shipments").RequireAuthorization().RequireFormToken();

        staff.MapGet("/", async (HttpContext http, Guid? warehouse, string? view, string? service, string? country, string? search, int? page, ListOperationsShipmentsUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ListOperationsShipmentsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http),
                Request(warehouse, view, service, country, search, page, false), http.RequestAborted);
            http.Response.Headers.CacheControl = "no-store";
            return OperationsStaffResults.Page<ShipmentsPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                Rows = response.Rows,
                TotalCount = response.TotalCount,
                Counts = response.Counts,
                Warehouses = response.Warehouses,
                Warehouse = warehouse,
                View = View(view),
                Service = service,
                Country = country,
                Search = search,
                PageNumber = Math.Max(page ?? 1, 1),
                Error = errors.Describe(response.Error)
            });
        });

        staff.MapGet("/export", async (HttpContext http, Guid? warehouse, string? view, string? service, string? country, string? search, ListOperationsShipmentsUseCase list) =>
        {
            ListOperationsShipmentsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http),
                Request(warehouse, view, service, country, search, 1, true), http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            Dictionary<Guid, string> codes = response.Warehouses.ToDictionary(item => item.Id, item => item.Code);
            StringBuilder csv = new();
            Line(csv, new[] { "number", "origin", "customer_code", "status", "country", "city", "recipient", "service", "announced", "parcels", "labelled", "created_utc" });

            foreach (OperationsShipmentRow row in response.Rows)
            {
                Line(csv, new[]
                {
                    row.Number, codes.GetValueOrDefault(row.OriginWarehouseId), row.CustomerCode, row.Status.ToString(), row.CountryCode, row.City, row.RecipientName, row.ServiceCode,
                    row.Announced.ToString(CultureInfo.InvariantCulture), row.Parcels.ToString(CultureInfo.InvariantCulture), row.Labelled.ToString(CultureInfo.InvariantCulture),
                    row.CreatedUtc.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                });
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", "shipments.csv");
        });

        staff.MapGet("/{shipmentId:guid}", async (HttpContext http, Guid shipmentId, string? done, GetOperationsShipmentUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await ShipmentAsync(http, shipmentId, null, done == "service" ? text["ui.shipments_desk.done.service"].Value : null, get, brand, errors));

        staff.MapPost("/{shipmentId:guid}/service", async (HttpContext http, Guid shipmentId, [FromForm] string? service, ChangeOperationsShipmentServiceUseCase change,
            GetOperationsShipmentUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ChangeOperationsShipmentServiceResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ChangeOperationsShipmentServiceRequest { ShipmentId = shipmentId, ServiceCode = service }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/shipments/" + shipmentId + "?done=service")
                : await ShipmentAsync(http, shipmentId, response.Error, null, get, brand, errors);
        });

        staff.MapGet("/{shipmentId:guid}/documents/{kind}", async (HttpContext http, Guid shipmentId, string kind, GetOperationsShipmentDocumentUseCase document) =>
        {
            GetOperationsShipmentDocumentResponse response = await document.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsShipmentDocumentRequest
            {
                ShipmentId = shipmentId,
                Kind = kind switch { "packing-list" => OperationsShipmentDocumentKind.PackingList, "commercial-invoice" => OperationsShipmentDocumentKind.CommercialInvoice, _ => 0 }
            }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(response.Content, "application/pdf", response.FileName);
        });

        return endpoints;
    }

    private static async Task<IResult> ShipmentAsync(HttpContext http, Guid shipmentId, Error? error, string? info, GetOperationsShipmentUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsShipmentResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsShipmentRequest { ShipmentId = shipmentId }, http.RequestAborted);

        if (response.Shipment is not { } shipment)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<ShipmentPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Shipment = shipment,
            Origin = response.Origin,
            DestinationName = response.DestinationName,
            CustomerCode = response.CustomerCode,
            CustomerName = response.CustomerName,
            PreAlerts = response.PreAlerts,
            Parcels = response.Parcels,
            Services = response.Services,
            CanChangeService = response.CanChangeService,
            Settings = response.Settings,
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static ListOperationsShipmentsRequest Request(Guid? warehouse, string? view, string? service, string? country, string? search, int? page, bool export) => new()
    {
        WarehouseId = warehouse,
        View = View(view),
        ServiceCode = service,
        CountryCode = country,
        Search = search,
        Page = Math.Max(page ?? 1, 1),
        Export = export
    };

    private static OperationsShipmentView View(string? value) => value switch
    {
        "waiting" => OperationsShipmentView.Waiting,
        "cancelled" => OperationsShipmentView.Cancelled,
        "all" => OperationsShipmentView.All,
        _ => OperationsShipmentView.ToSend
    };

    private static void Line(StringBuilder csv, IEnumerable<string?> cells) =>
        csv.Append(string.Join(',', cells.Select(OperationsIncomingEndpoints.Cell))).Append("\r\n");
}
