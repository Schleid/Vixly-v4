using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Items.ChangeOperationsItemRule;
using Vixly.Operations.Application.UseCases.Items.GetOperationsItemCatalogue;
using Vixly.Operations.Application.UseCases.Items.ListOperationsItemsToReview;
using Vixly.Operations.Application.UseCases.Items.RemoveOperationsItemCatalogueEntry;
using Vixly.Operations.Application.UseCases.Items.SaveOperationsItemCatalogueEntry;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Items;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Items;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsItemEndpoints.cs
//
// PURPOSE:
// Items to review and the item catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - /staff/items: the parcels whose items need a customs code or origin
// - /staff/settings/items: the catalogue and the rule before bundling
//
// IMPORTANT NOTES:
// - Static forms with antiforgery (ADR-0019); the use cases check the permissions
// - The items of one parcel are classified on its page (/staff/parcels/{id}/items)
// =========================================================
public static class OperationsItemEndpoints
{
    public static IEndpointRouteBuilder MapOperationsItemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Every change under /staff checks the antiforgery token, also when the form carries no other field (ADR-0019).
        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/items", async (HttpContext http, ListOperationsItemsToReviewUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ListOperationsItemsToReviewResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsItemsToReviewRequest(), http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "no-store";
            return new RazorComponentResult<ItemsReviewPage>(new
            {
                Company = await CompanyAsync(http, brand),
                Parcels = response.Parcels,
                Required = response.Required
            });
        });

        staff.MapGet("/settings/items", async (HttpContext http, string? q, string? done, GetOperationsItemCatalogueUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await CatalogueAsync(http, q, null, done is null ? null : text["ui.staff.done.saved"].Value, get, brand, errors));

        staff.MapPost("/settings/items", async (HttpContext http, [FromForm] string? description, [FromForm] string? matches, [FromForm] string? code, [FromForm] string? unit,
            SaveOperationsItemCatalogueEntryUseCase save, GetOperationsItemCatalogueUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await SaveAsync(http, null, description, matches, code, unit, save, get, brand, errors));

        staff.MapPost("/settings/items/{entryId:guid}", async (HttpContext http, Guid entryId, [FromForm] string? description, [FromForm] string? matches, [FromForm] string? code,
            [FromForm] string? unit, SaveOperationsItemCatalogueEntryUseCase save, GetOperationsItemCatalogueUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await SaveAsync(http, entryId, description, matches, code, unit, save, get, brand, errors));

        staff.MapPost("/settings/items/{entryId:guid}/remove", async (HttpContext http, Guid entryId, RemoveOperationsItemCatalogueEntryUseCase remove,
            GetOperationsItemCatalogueUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            RemoveOperationsItemCatalogueEntryResponse response = await remove.ExecuteAsync(OperationsCurrentUser.Context(http),
                new RemoveOperationsItemCatalogueEntryRequest { EntryId = entryId }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/items?done=1") : await CatalogueAsync(http, null, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/settings/items/rule", async (HttpContext http, [FromForm] bool? required, ChangeOperationsItemRuleUseCase change, GetOperationsItemCatalogueUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ChangeOperationsItemRuleResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ChangeOperationsItemRuleRequest { Required = required == true }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/items?done=1") : await CatalogueAsync(http, null, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    private static async Task<IResult> SaveAsync(HttpContext http, Guid? entryId, string? description, string? matches, string? code, string? unit,
        SaveOperationsItemCatalogueEntryUseCase save, GetOperationsItemCatalogueUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        SaveOperationsItemCatalogueEntryResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsItemCatalogueEntryRequest
        {
            EntryId = entryId,
            Description = description,
            Matches = matches,
            HsCode = code,
            Unit = Enum.TryParse(unit, false, out OperationsItemUnit chosen) ? chosen : 0
        }, http.RequestAborted);

        return response.Succeeded ? Results.Redirect("/staff/settings/items?done=1") : await CatalogueAsync(http, null, response.Error, null, get, brand, errors);
    }

    private static async Task<IResult> CatalogueAsync(HttpContext http, string? search, Error? error, string? info, GetOperationsItemCatalogueUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsItemCatalogueResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsItemCatalogueRequest(), http.RequestAborted);

        if (!response.Succeeded)
        {
            return Results.NotFound();
        }

        http.Response.Headers.CacheControl = "no-store";
        return new RazorComponentResult<ItemCataloguePage>(new
        {
            Company = await CompanyAsync(http, brand),
            Entries = response.Entries,
            RequiredSinceUtc = response.RequiredSinceUtc,
            CanEdit = response.CanEdit,
            Search = search?.Trim(),
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static async Task<string?> CompanyAsync(HttpContext http, GetOperationsSignInBrandUseCase brand) =>
        (await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted)).DisplayName;
}
