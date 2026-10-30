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

    private static async Task<IResult> PageAsync(HttpContext http, Guid warehouseId, Error? error, string? info, string? newKey, ListOperationsPrintersUseCase list,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
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
            Error = errors.Describe(error),
            Info = info
        });
    }
}
