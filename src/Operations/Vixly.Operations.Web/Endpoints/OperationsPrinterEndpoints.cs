using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.UseCases.Printing.ListOperationsPrinters;
using Vixly.Operations.Application.UseCases.Printing.PrintOperationsParcelLabel;
using Vixly.Operations.Application.UseCases.Printing.RegisterOperationsPrintAgent;
using Vixly.Operations.Application.UseCases.Printing.SaveOperationsPrinter;
using Vixly.Operations.Application.UseCases.Printing.StopOperationsPrintAgent;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Printing;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Parcels;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsPrinterEndpoints.cs
//
// PURPOSE:
// The label printers and dock print programs of a warehouse (Slice 3B).
//
// RESPONSIBILITIES:
// - /staff/locations/warehouse/{id}/printers: list, add or change a printer, print a test label, register or stop a print program
//
// IMPORTANT NOTES:
// - A new print program key is shown once, on the answer to the form that made it; it is never stored in clear
// =========================================================
public static class OperationsPrinterEndpoints
{
    public static IEndpointRouteBuilder MapOperationsPrinterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff/locations/warehouse/{warehouseId:guid}/printers").RequireAuthorization().RequireFormToken();

        staff.MapGet("/", async (HttpContext http, Guid warehouseId, string? done, ListOperationsPrintersUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) =>
            await PageAsync(http, warehouseId, null, done is "saved" or "tested" or "stopped" ? text["ui.printers.done." + done].Value : null, null, list, brand, errors));

        staff.MapPost("/", async (HttpContext http, Guid warehouseId, [FromForm] Guid? printer, [FromForm] string? name, [FromForm] string? connection, [FromForm] string? address,
            [FromForm] int? dpi, [FromForm] string? active, [FromForm] string? media, SaveOperationsPrinterUseCase save, ListOperationsPrintersUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            SaveOperationsPrinterResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsPrinterRequest
            {
                WarehouseId = warehouseId,
                PrinterId = printer,
                Name = name,
                Connection = connection == "installed" ? OperationsPrinterConnection.Installed : OperationsPrinterConnection.Network,
                Address = address,
                Dpi = dpi ?? 0,
                Media = media == "shipping" ? Vixly.Operations.Domain.Printing.OperationsLabelMedia.ShippingLabel : Vixly.Operations.Domain.Printing.OperationsLabelMedia.IdLabel,
                Active = printer is null || active == "true"
            }, http.RequestAborted);

            return response.Succeeded ? Results.Redirect(Path(warehouseId) + "?done=saved") : await PageAsync(http, warehouseId, response.Error, null, null, list, brand, errors);
        });

        staff.MapPost("/{printerId:guid}/test", async (HttpContext http, Guid warehouseId, Guid printerId, PrintOperationsParcelLabelUseCase print, ListOperationsPrintersUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            PrintOperationsParcelLabelResponse response = await print.ExecuteAsync(OperationsCurrentUser.Context(http), new PrintOperationsParcelLabelRequest { PrinterId = printerId }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect(Path(warehouseId) + "?done=tested") : await PageAsync(http, warehouseId, response.Error, null, null, list, brand, errors);
        });

        staff.MapPost("/agents", async (HttpContext http, Guid warehouseId, [FromForm] string? name, RegisterOperationsPrintAgentUseCase register, ListOperationsPrintersUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            RegisterOperationsPrintAgentResponse response = await register.ExecuteAsync(OperationsCurrentUser.Context(http),
                new RegisterOperationsPrintAgentRequest { WarehouseId = warehouseId, Name = name }, http.RequestAborted);
            return await PageAsync(http, warehouseId, response.Error, null, response.Succeeded ? response.Key : null, list, brand, errors);
        });

        // Slice 12E (owner H-6): a one-time code for a phone running Vixly Print, shown as a QR code.
        staff.MapPost("/agents/phone", async (HttpContext http, Guid warehouseId, [FromForm] string? name,
            Vixly.Operations.Application.UseCases.Printing.CreateOperationsPrintPairing.CreateOperationsPrintPairingUseCase create, ListOperationsPrintersUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Vixly.Operations.Application.UseCases.Printing.CreateOperationsPrintPairing.CreateOperationsPrintPairingResponse response = await create.ExecuteAsync(
                OperationsCurrentUser.Context(http),
                new Vixly.Operations.Application.UseCases.Printing.CreateOperationsPrintPairing.CreateOperationsPrintPairingRequest { WarehouseId = warehouseId, Name = name }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return await PageAsync(http, warehouseId, response.Error, null, null, list, brand, errors);
            }

            string link = "https://" + http.Request.Host.Value + "/agent/v1/pair?code=" + response.Code;
            return await PageAsync(http, warehouseId, null, null, null, list, brand, errors, new PhonePairingView(response.Code, Qr(link), response.ExpiresUtc));
        });

        staff.MapPost("/agents/{agentId:guid}/stop", async (HttpContext http, Guid warehouseId, Guid agentId, StopOperationsPrintAgentUseCase stop, ListOperationsPrintersUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            StopOperationsPrintAgentResponse response = await stop.ExecuteAsync(OperationsCurrentUser.Context(http),
                new StopOperationsPrintAgentRequest { WarehouseId = warehouseId, AgentId = agentId }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect(Path(warehouseId) + "?done=stopped") : await PageAsync(http, warehouseId, response.Error, null, null, list, brand, errors);
        });

        return endpoints;
    }

    private static string Path(Guid warehouseId) => "/staff/locations/warehouse/" + warehouseId + "/printers";

    private static string Qr(string text)
    {
        using QRCoder.QRCodeGenerator generator = new();
        using QRCoder.QRCodeData data = generator.CreateQrCode(text, QRCoder.QRCodeGenerator.ECCLevel.M);
        return "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(new QRCoder.SvgQRCode(data).GetGraphic(5)));
    }

    private static async Task<IResult> PageAsync(HttpContext http, Guid warehouseId, Error? error, string? info, string? newKey, ListOperationsPrintersUseCase list,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, PhonePairingView? pairing = null)
    {
        ListOperationsPrintersResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsPrintersRequest { WarehouseId = warehouseId }, http.RequestAborted);

        if (response.Warehouse is not { } warehouse)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<PrintersPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Warehouse = warehouse,
            Printers = response.Printers,
            Agents = response.Agents,
            NewKey = newKey,
            Pairing = pairing,
            PrintAppName = http.RequestServices.GetRequiredService<IConfiguration>()["OperationsPrintHelper:AppName"] ?? string.Empty,
            Error = errors.Describe(error),
            Info = info
        });
    }
}
