using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Api.CreateOperationsApiKey;
using Vixly.Operations.Application.UseCases.Api.GetOperationsApiSettings;
using Vixly.Operations.Application.UseCases.Api.ResendOperationsWebhookDelivery;
using Vixly.Operations.Application.UseCases.Api.RevokeOperationsApiKey;
using Vixly.Operations.Application.UseCases.Api.SaveOperationsWebhookEndpoint;
using Vixly.Operations.Application.UseCases.Api.SendOperationsWebhookTest;
using Vixly.Operations.Application.UseCases.Api.SwitchOperationsWebhookEndpoint;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Api;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.ApiSettings;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsApiSettingsEndpoints.cs
//
// PURPOSE:
// Settings → API and webhooks (Slice 10): keys and webhook addresses of the company.
//
// RESPONSIBILITIES:
// - /staff/settings/api and its forms: make and revoke keys; add, change, test, turn off or on and remove addresses; send a delivery again
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); making a key and adding or changing an address ask for the second check
// - A new key or secret is shown in the answer page once, never in an address
// =========================================================
public static class OperationsApiSettingsEndpoints
{
    private const string Page = "/staff/settings/api";

    public static IEndpointRouteBuilder MapOperationsApiSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup(Page).RequireAuthorization().RequireFormToken();

        staff.MapGet("", async (HttpContext http, string? done, GetOperationsApiSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) => await PageAsync(http, null, done is null ? null : text["ui.staff.done.saved"].Value, null, get, brand, errors));

        staff.MapPost("/keys", async (HttpContext http, [FromForm] string? name, CreateOperationsApiKeyUseCase create, GetOperationsApiSettingsUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
        {
            CreateOperationsApiKeyResponse response = await create.ExecuteAsync(OperationsCurrentUser.Context(http), new CreateOperationsApiKeyRequest { Name = name }, http.RequestAborted);
            return response.Succeeded ? await PageAsync(http, null, text["ui.api.key_made"].Value, response.Token, get, brand, errors)
                : OperationsStaffEndpoints.StepUp(response.Error, Page) ?? await PageAsync(http, response.Error, null, null, get, brand, errors);
        });

        staff.MapPost("/keys/{keyId:guid}/revoke", async (HttpContext http, Guid keyId, RevokeOperationsApiKeyUseCase revoke, GetOperationsApiSettingsUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            RevokeOperationsApiKeyResponse response = await revoke.ExecuteAsync(OperationsCurrentUser.Context(http), new RevokeOperationsApiKeyRequest { KeyId = keyId }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect(Page + "?done=1#keys") : await PageAsync(http, response.Error, null, null, get, brand, errors);
        });

        staff.MapPost("/webhooks", (HttpContext http, SaveOperationsWebhookEndpointUseCase save, GetOperationsApiSettingsUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) => SaveAsync(http, null, save, get, brand, errors, text));

        staff.MapPost("/webhooks/{endpointId:guid}", (HttpContext http, Guid endpointId, SaveOperationsWebhookEndpointUseCase save, GetOperationsApiSettingsUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) => SaveAsync(http, endpointId, save, get, brand, errors, text));

        staff.MapPost("/webhooks/{endpointId:guid}/switch", async (HttpContext http, Guid endpointId, string? action, SwitchOperationsWebhookEndpointUseCase change,
            GetOperationsApiSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            SwitchOperationsWebhookEndpointResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http),
                new SwitchOperationsWebhookEndpointRequest { EndpointId = endpointId, Action = action ?? string.Empty }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect(Page + "?done=1#webhooks") : await PageAsync(http, response.Error, null, null, get, brand, errors);
        });

        staff.MapPost("/webhooks/{endpointId:guid}/test", async (HttpContext http, Guid endpointId, SendOperationsWebhookTestUseCase test, GetOperationsApiSettingsUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
        {
            SendOperationsWebhookTestResponse response = await test.ExecuteAsync(OperationsCurrentUser.Context(http), new SendOperationsWebhookTestRequest { EndpointId = endpointId },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                return await PageAsync(http, response.Error, null, null, get, brand, errors);
            }

            string result = response.Delivered
                ? VixlyText.Format(text, "ui.api.test_delivered", "status", response.StatusCode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty)
                : VixlyText.Format(text, "ui.api.test_failed", "reason", response.StatusCode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? response.Problem ?? string.Empty);
            return await PageAsync(http, null, result, null, get, brand, errors);
        });

        staff.MapPost("/deliveries/{deliveryId:guid}/resend", async (HttpContext http, Guid deliveryId, ResendOperationsWebhookDeliveryUseCase resend,
            GetOperationsApiSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ResendOperationsWebhookDeliveryResponse response = await resend.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ResendOperationsWebhookDeliveryRequest { DeliveryId = deliveryId }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect(Page + "?done=1#deliveries") : await PageAsync(http, response.Error, null, null, get, brand, errors);
        });

        return endpoints;
    }

    private static async Task<IResult> SaveAsync(HttpContext http, Guid? endpointId, SaveOperationsWebhookEndpointUseCase save, GetOperationsApiSettingsUseCase get,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text)
    {
        IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
        OperationsWebhookEvents events = OperationsWebhookEvents.None;

        foreach (string? value in form["events"])
        {
            if (Enum.TryParse(value, false, out OperationsWebhookEvents kind) && OperationsWebhookEventNames.All.Contains(kind))
            {
                events |= kind;
            }
        }

        SaveOperationsWebhookEndpointResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http),
            new SaveOperationsWebhookEndpointRequest { EndpointId = endpointId, Url = form["url"].ToString(), Events = events }, http.RequestAborted);

        if (!response.Succeeded)
        {
            return OperationsStaffEndpoints.StepUp(response.Error, Page) ?? await PageAsync(http, response.Error, null, null, get, brand, errors);
        }

        return response.Secret is { } secret ? await PageAsync(http, null, text["ui.api.webhook_added"].Value, secret, get, brand, errors) : Results.Redirect(Page + "?done=1#webhooks");
    }

    private static async Task<IResult> PageAsync(HttpContext http, Error? error, string? info, string? shown, GetOperationsApiSettingsUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsApiSettingsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsApiSettingsRequest(), http.RequestAborted);

        // A page that carries a secret is never kept by a browser or a proxy.
        if (shown is not null)
        {
            http.Response.Headers.CacheControl = "no-store";
        }

        return OperationsStaffResults.Page<ApiSettingsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.InPlan,
            response.Keys,
            response.Endpoints,
            response.Deliveries,
            response.CanEdit,
            Shown = shown,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
