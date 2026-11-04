using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.UseCases.Receiving.ListOperationsIncoming;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Parcels;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsIncomingEndpoints.cs
//
// PURPOSE:
// The incoming packages list for staff, and its CSV export (Slice 4D, A-7).
//
// RESPONSIBILITIES:
// - /staff/parcels/incoming: every pre-alert of the warehouses the person may see, filtered by view, expected days, carrier and search
// - /staff/parcels/incoming/export: the same rows as a file
//
// IMPORTANT NOTES:
// - The use case checks the permission (parcels: view) and the warehouse scope
// - Customer text in the file never starts a formula (=, +, -, @, tab, return): such a cell is prefixed with an apostrophe
// - Nothing is cached (no-store)
// =========================================================
public static class OperationsIncomingEndpoints
{
    public static IEndpointRouteBuilder MapOperationsIncomingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff/parcels").RequireAuthorization();

        staff.MapGet("/incoming", async (HttpContext http, Guid? warehouse, string? view, string? from, string? to, string? carrier, string? search, int? page,
            ListOperationsIncomingUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsIncomingQuery query = new(warehouse, View(view), Day(from), Day(to), carrier, search, Math.Max(page ?? 1, 1));
            ListOperationsIncomingResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), Request(query, false), http.RequestAborted);

            http.Response.Headers.CacheControl = "no-store";
            return new RazorComponentResult<IncomingPage>(new
            {
                Company = (await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted)).DisplayName,
                Query = query,
                Rows = response.Rows,
                TotalCount = response.TotalCount,
                Counts = response.Counts,
                Warehouses = response.Warehouses,
                Today = response.Today,
                Error = errors.Describe(response.Error)
            });
        });

        staff.MapGet("/incoming/export", async (HttpContext http, Guid? warehouse, string? view, string? from, string? to, string? carrier, string? search,
            ListOperationsIncomingUseCase list, IStringLocalizer<OperationsText> text) =>
        {
            OperationsIncomingQuery query = new(warehouse, View(view), Day(from), Day(to), carrier, search, 1);
            ListOperationsIncomingResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), Request(query, true), http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            Dictionary<Guid, string> codes = response.Warehouses.ToDictionary(item => item.Id, item => item.Code);
            StringBuilder csv = new();
            Line(csv, new[] { "warehouse", "customer_code", "customer", "carrier", "tracking", "store", "contents", "value", "currency", "expected", "status", "parcel", "in_shipment", "announced_utc" });

            foreach (OperationsIncomingRow row in response.Rows)
            {
                Line(csv, new[]
                {
                    row.WarehouseId is { } id ? codes.GetValueOrDefault(id) : null, row.CustomerCode, row.CustomerName, row.Carrier, row.TrackingNumber, row.Store, row.Contents,
                    row.DeclaredValue.ToString("0.00", CultureInfo.InvariantCulture), row.CurrencyCode, row.ExpectedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    row.Status.ToString(), row.ParcelNumber, row.InShipment ? "yes" : "no", row.CreatedUtc.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                });
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8",
                "incoming-" + response.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".csv");
        });

        return endpoints;
    }

    private static ListOperationsIncomingRequest Request(OperationsIncomingQuery query, bool export) => new()
    {
        WarehouseId = query.WarehouseId,
        View = query.View,
        From = query.From,
        To = query.To,
        Carrier = query.Carrier,
        Search = query.Search,
        Page = query.Page,
        Export = export
    };

    private static OperationsIncomingView View(string? value) =>
        Enum.TryParse(value, true, out OperationsIncomingView view) && Enum.IsDefined(view) ? view : OperationsIncomingView.Expected;

    private static DateOnly? Day(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day) ? day : null;

    private static void Line(StringBuilder csv, IEnumerable<string?> cells) =>
        csv.Append(string.Join(',', cells.Select(Cell))).Append("\r\n");

    // Quoted; a cell that a spreadsheet would read as a formula is made plain text (CSV injection).
    internal static string Cell(string? value)
    {
        string text = value ?? string.Empty;

        if (text.Length > 0 && "=+-@\t\r".Contains(text[0], StringComparison.Ordinal))
        {
            text = "'" + text;
        }

        return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}

