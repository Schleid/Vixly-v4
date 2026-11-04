using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Vixly.Contracts.Operations.Printing;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.UseCases.Printing.FinishOperationsPrintJob;
using Vixly.Operations.Application.UseCases.Printing.TakeOperationsPrintJobs;
using Vixly.Operations.Domain.Printing;
using Vixly.Operations.Web.Tenancy;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsPrintAgentEndpoints.cs
//
// PURPOSE:
// The calls of the dock print program (Slice 3B).
//
// RESPONSIBILITIES:
// - GET /agent/v1/print-jobs: the labels waiting for the warehouse's printers
// - POST /agent/v1/print-jobs/{id}: printed or not
//
// IMPORTANT NOTES:
// - The key travels as a bearer token; no cookie, no antiforgery, no staff session
// - On the company host, so the key only works for its own company
// =========================================================
public static class OperationsPrintAgentEndpoints
{
    public static IEndpointRouteBuilder MapOperationsPrintAgentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder agent = endpoints.MapGroup("/agent/v1/print-jobs");

        agent.MapGet("/", async (HttpContext http, TakeOperationsPrintJobsUseCase take) =>
        {
            if (Context(http) is not { } context)
            {
                return Results.NotFound();
            }

            TakeOperationsPrintJobsResponse response = await take.ExecuteAsync(context, new TakeOperationsPrintJobsRequest { AgentKey = Key(http) }, http.RequestAborted);
            http.Response.Headers.CacheControl = "no-store";

            return response.Succeeded
                ? Results.Ok(response.Jobs.Select(job => new PrintJobDto(job.JobId, job.Connection switch { OperationsPrinterConnection.Installed => "installed", OperationsPrinterConnection.Bluetooth => "bluetooth", _ => "network" }, job.Address, job.Payload, job.ExpiresInSeconds)).ToList())
                : Results.Unauthorized();
        });

        agent.MapPost("/{jobId:guid}", async (HttpContext http, Guid jobId, [FromBody] PrintJobResultBody body, FinishOperationsPrintJobUseCase finish) =>
        {
            if (Context(http) is not { } context)
            {
                return Results.NotFound();
            }

            FinishOperationsPrintJobResponse response = await finish.ExecuteAsync(context, new FinishOperationsPrintJobRequest
            {
                AgentKey = Key(http),
                JobId = jobId,
                Printed = body.Printed,
                ErrorCode = body.ErrorCode
            }, http.RequestAborted);

            return response.Succeeded ? Results.NoContent() : Results.Unauthorized();
        });

        // Slice 12E (owner H-6): Vixly Print joins with the one-time code of the Printers page, then saves the portable printer next to it.
        endpoints.MapPost("/agent/v1/pair", async (HttpContext http, [FromBody] PhonePairBody body,
            Vixly.Operations.Application.UseCases.Printing.PairOperationsPrintAgent.PairOperationsPrintAgentUseCase pair) =>
        {
            if (Context(http) is not { } context)
            {
                return Results.NotFound();
            }

            Vixly.Operations.Application.UseCases.Printing.PairOperationsPrintAgent.PairOperationsPrintAgentResponse response = await pair.ExecuteAsync(context,
                new Vixly.Operations.Application.UseCases.Printing.PairOperationsPrintAgent.PairOperationsPrintAgentRequest { Code = body.Code, DeviceName = body.Device }, http.RequestAborted);
            http.Response.Headers.CacheControl = "no-store";
            return response.Succeeded ? Results.Ok(new PhonePairDto(response.AgentId, response.Key, response.WarehouseName)) : Results.BadRequest();
        }).RequireRateLimiting(Vixly.Operations.Web.Hosting.OperationsRateLimits.SignIn);

        endpoints.MapPost("/agent/v1/printers", async (HttpContext http, [FromBody] PhonePrinterBody body,
            Vixly.Operations.Application.UseCases.Printing.SaveOperationsPhonePrinter.SaveOperationsPhonePrinterUseCase save) =>
        {
            if (Context(http) is not { } context)
            {
                return Results.NotFound();
            }

            Vixly.Operations.Application.UseCases.Printing.SaveOperationsPhonePrinter.SaveOperationsPhonePrinterResponse response = await save.ExecuteAsync(context,
                new Vixly.Operations.Application.UseCases.Printing.SaveOperationsPhonePrinter.SaveOperationsPhonePrinterRequest
                {
                    AgentKey = Key(http),
                    Name = body.Name,
                    Address = body.Address,
                    Dpi = body.Dpi,
                    Media = body.Media == "shipping" ? OperationsLabelMedia.ShippingLabel : OperationsLabelMedia.IdLabel,
                    Active = body.Active
                }, http.RequestAborted);
            return response.Succeeded ? Results.Ok(new PhonePrinterDto(response.PrinterId))
                : response.Error?.Code == OperationsPrintingErrors.AgentNotFound.Code ? Results.Unauthorized() : Results.BadRequest();
        });

        return endpoints;
    }

    private static OperationsAuthorizationContext? Context(HttpContext http) =>
        OperationsTenantMiddleware.Host(http) is { TenantId: { } tenantId } ? OperationsAuthorizationContext.ForTenant(tenantId) : null;

    private static string Key(HttpContext http) =>
        AuthenticationHeaderValue.TryParse(http.Request.Headers.Authorization.ToString(), out AuthenticationHeaderValue? header) && header.Scheme == "Bearer" ? header.Parameter ?? string.Empty : string.Empty;
}
