using Microsoft.AspNetCore.Http.HttpResults;
using Vixly.Operations.Application.UseCases.Locations.ListOperationsLocations;
using Vixly.Operations.Application.UseCases.Locations.RemoveOperationsCountryContact;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsBranch;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsCountryContact;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsWarehouse;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.Site.GetOperationsSiteDraft;
using Vixly.Operations.Application.UseCases.Site.PublishOperationsSite;
using Vixly.Operations.Application.UseCases.Site.RemoveOperationsSiteImage;
using Vixly.Operations.Application.UseCases.Site.UpdateOperationsSiteDraft;
using Vixly.Operations.Application.UseCases.Site.UploadOperationsSiteImage;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Web.Components.Editor;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Operations.Web.Site;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsEditorEndpoints.cs
//
// PURPOSE:
// The Owner pages that edit the public site, the locations and the contacts per country (Slice 1B).
//
// RESPONSIBILITIES:
// - /staff/site: brand, texts per language, images, preview and publish
// - /staff/locations: branches, warehouses and contacts per country
//
// IMPORTANT NOTES:
// - Signed-in staff only; the use cases refuse anyone but the Owner (ops.access.owner_only)
// - Static forms with antiforgery (ADR-0019); a successful post redirects (no double submit on refresh)
// - Uploads larger than an image may be are refused before they are read
// =========================================================
public static class OperationsEditorEndpoints
{
    // An image is at most 1 MB (OperationsSiteImage.MaxBytes); the form around it adds a little.
    private const long UploadLimit = OperationsSiteImage.MaxBytes + (64 * 1024);

    public static IEndpointRouteBuilder MapOperationsEditorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Every change under /staff checks the antiforgery token, also when the form carries no other field (ADR-0019).
        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/site", async (HttpContext http, string? language, string? saved, GetOperationsSiteDraftUseCase useCase, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
            await SitePageAsync(http, Language(language), useCase, await CompanyAsync(http, brand), text, null, saved is null ? null : Saved(http, saved)));

        staff.MapPost("/site", async (HttpContext http, string? language, IFormCollection form, UpdateOperationsSiteDraftUseCase update, GetOperationsSiteDraftUseCase draft, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            string lang = Language(language);
            UpdateOperationsSiteDraftResponse response = await update.ExecuteAsync(OperationsCurrentUser.Context(http), OperationsEditorForms.Site(form, lang), http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/site?language=" + lang + "&saved=draft")
                : await SitePageAsync(http, lang, draft, await CompanyAsync(http, brand), text, response.Error, null);
        });

        staff.MapPost("/site/publish", async (HttpContext http, IFormCollection form, PublishOperationsSiteUseCase publish, GetOperationsSiteDraftUseCase draft, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            PublishOperationsSiteResponse response = await publish.ExecuteAsync(OperationsCurrentUser.Context(http), new PublishOperationsSiteRequest(), http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/site?saved=published")
                : await SitePageAsync(http, Language(null), draft, await CompanyAsync(http, brand), text, response.Error, null);
        });

        staff.MapPost("/site/image/{kind}", async (HttpContext http, string kind, IFormFile? file, UploadOperationsSiteImageUseCase upload, GetOperationsSiteDraftUseCase draft, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            if (OperationsEditorForms.ImageKind(kind) is not { } imageKind)
            {
                return Results.NotFound();
            }

            Error? error = null;

            if (file is null || file.Length == 0)
            {
                error = OperationsBrandingErrors.ImageTypeNotAllowed;
            }
            else if (file.Length > OperationsSiteImage.MaxBytes)
            {
                error = OperationsBrandingErrors.ImageTooLarge;
            }
            else
            {
                using MemoryStream buffer = new((int)file.Length);
                await file.CopyToAsync(buffer, http.RequestAborted);

                UploadOperationsSiteImageResponse response = await upload.ExecuteAsync(
                    OperationsCurrentUser.Context(http), new UploadOperationsSiteImageRequest { Kind = imageKind, Content = buffer.ToArray() }, http.RequestAborted);

                if (response.Succeeded)
                {
                    return Results.Redirect("/staff/site?saved=image");
                }

                error = response.Error;
            }

            return await SitePageAsync(http, Language(null), draft, await CompanyAsync(http, brand), text, error, null);
        }).WithMetadata(new OperationsUploadLimit(UploadLimit)).WithFormOptions(multipartBodyLengthLimit: UploadLimit);

        staff.MapPost("/site/image/{kind}/remove", async (HttpContext http, string kind, IFormCollection form, RemoveOperationsSiteImageUseCase remove, GetOperationsSiteDraftUseCase draft, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            if (OperationsEditorForms.ImageKind(kind) is not { } imageKind)
            {
                return Results.NotFound();
            }

            RemoveOperationsSiteImageResponse response = await remove.ExecuteAsync(OperationsCurrentUser.Context(http), new RemoveOperationsSiteImageRequest { Kind = imageKind }, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/site?saved=image")
                : await SitePageAsync(http, Language(null), draft, await CompanyAsync(http, brand), text, response.Error, null);
        });

        staff.MapGet("/locations", async (HttpContext http, string? saved, ListOperationsLocationsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
            await LocationsPageAsync(http, list, await CompanyAsync(http, brand), text, null, saved is null ? null : Saved(http, "location")));

        staff.MapGet("/locations/branch/{branchId:guid?}", async (HttpContext http, Guid? branchId, ListOperationsLocationsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            ListOperationsLocationsResponse locations = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsLocationsRequest(), http.RequestAborted);

            if (!locations.Succeeded)
            {
                return await LocationsPageAsync(http, list, await CompanyAsync(http, brand), text, locations.Error, null);
            }

            if (branchId is null)
            {
                return Page<BranchFormPage>(new { Company = await CompanyAsync(http, brand), Form = new SaveOperationsBranchRequest() });
            }

            return locations.Branches.FirstOrDefault(branch => branch.Id == branchId) is { } found
                ? Page<BranchFormPage>(new { Company = await CompanyAsync(http, brand), Form = OperationsEditorForms.Branch(found) })
                : Results.NotFound();
        });

        staff.MapPost("/locations/branch", async (HttpContext http, IFormCollection form, SaveOperationsBranchUseCase save, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            SaveOperationsBranchRequest request = OperationsEditorForms.Branch(form);
            SaveOperationsBranchResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), request, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/locations?saved=1")
                : Page<BranchFormPage>(new { Company = await CompanyAsync(http, brand), Form = request, Error = text.Describe(response.Error) });
        });

        staff.MapGet("/locations/warehouse/{warehouseId:guid?}", async (HttpContext http, Guid? warehouseId, ListOperationsLocationsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            ListOperationsLocationsResponse locations = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsLocationsRequest(), http.RequestAborted);

            if (!locations.Succeeded)
            {
                return await LocationsPageAsync(http, list, await CompanyAsync(http, brand), text, locations.Error, null);
            }

            if (warehouseId is null)
            {
                return Page<WarehouseFormPage>(new { Company = await CompanyAsync(http, brand), Branches = locations.Branches, Managers = locations.Managers, Warehouses = locations.Warehouses, Form = new SaveOperationsWarehouseRequest() });
            }

            return locations.Warehouses.FirstOrDefault(warehouse => warehouse.Id == warehouseId) is { } found
                ? Page<WarehouseFormPage>(new { Company = await CompanyAsync(http, brand), Branches = locations.Branches, Managers = locations.Managers, Warehouses = locations.Warehouses, Form = OperationsEditorForms.Warehouse(found) })
                : Results.NotFound();
        });

        staff.MapPost("/locations/warehouse", async (HttpContext http, IFormCollection form, SaveOperationsWarehouseUseCase save, ListOperationsLocationsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            SaveOperationsWarehouseRequest request = OperationsEditorForms.Warehouse(form);
            SaveOperationsWarehouseResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), request, http.RequestAborted);

            if (response.Succeeded)
            {
                return Results.Redirect("/staff/locations?saved=1");
            }

            ListOperationsLocationsResponse locations = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsLocationsRequest(), http.RequestAborted);
            return Page<WarehouseFormPage>(new { Company = await CompanyAsync(http, brand), Branches = locations.Branches, Managers = locations.Managers, Warehouses = locations.Warehouses, Form = request, Error = text.Describe(response.Error) });
        });

        staff.MapGet("/locations/contact/{contactId:guid?}", async (HttpContext http, Guid? contactId, ListOperationsLocationsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            ListOperationsLocationsResponse locations = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsLocationsRequest(), http.RequestAborted);

            if (!locations.Succeeded)
            {
                return await LocationsPageAsync(http, list, await CompanyAsync(http, brand), text, locations.Error, null);
            }

            if (contactId is null)
            {
                return Page<ContactFormPage>(new { Company = await CompanyAsync(http, brand), Form = new SaveOperationsCountryContactRequest() });
            }

            return locations.Contacts.FirstOrDefault(contact => contact.Id == contactId) is { } found
                ? Page<ContactFormPage>(new { Company = await CompanyAsync(http, brand), Form = OperationsEditorForms.Contact(found) })
                : Results.NotFound();
        });

        staff.MapPost("/locations/contact", async (HttpContext http, IFormCollection form, SaveOperationsCountryContactUseCase save, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            SaveOperationsCountryContactRequest request = OperationsEditorForms.Contact(form);
            SaveOperationsCountryContactResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), request, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/locations?saved=1")
                : Page<ContactFormPage>(new { Company = await CompanyAsync(http, brand), Form = request, Error = text.Describe(response.Error) });
        });

        staff.MapPost("/locations/contact/{contactId:guid}/remove", async (HttpContext http, Guid contactId, IFormCollection form, RemoveOperationsCountryContactUseCase remove, ListOperationsLocationsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            RemoveOperationsCountryContactResponse response = await remove.ExecuteAsync(OperationsCurrentUser.Context(http), new RemoveOperationsCountryContactRequest { ContactId = contactId }, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/locations?saved=1")
                : await LocationsPageAsync(http, list, await CompanyAsync(http, brand), text, response.Error, null);
        });

        return endpoints;
    }

    private static async Task<IResult> SitePageAsync(HttpContext http, string language, GetOperationsSiteDraftUseCase useCase, string? company, OperationsErrorText text, Error? error, string? info)
    {
        GetOperationsSiteDraftResponse draft = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSiteDraftRequest { Language = language }, http.RequestAborted);
        http.Response.Headers.CacheControl = "no-store";
        Vixly.Operations.Application.Ports.OperationsPlanFeatures features = OperationsCurrentUser.Context(http).TenantId is { } tenantId
            ? await http.RequestServices.GetRequiredService<Vixly.Operations.Application.Ports.IOperationsPlanFeatures>().GetAsync(tenantId, http.RequestAborted)
            : Vixly.Operations.Application.Ports.OperationsPlanFeatures.All;

        return Page<SiteEditorPage>(new
        {
            Company = company,
            Language = language,
            Draft = draft.Draft,
            Texts = draft.Texts,
            HasUnpublishedChanges = draft.HasUnpublishedChanges,
            Features = features,
            Error = text.Describe(error ?? draft.Error),
            Info = info
        });
    }

    private static async Task<IResult> LocationsPageAsync(HttpContext http, ListOperationsLocationsUseCase list, string? company, OperationsErrorText text, Error? error, string? info)
    {
        ListOperationsLocationsResponse locations = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsLocationsRequest(), http.RequestAborted);
        http.Response.Headers.CacheControl = "no-store";

        return Page<LocationsPage>(new
        {
            Company = company,
            Branches = locations.Branches,
            Warehouses = locations.Warehouses,
            Contacts = locations.Contacts,
            Error = text.Describe(error ?? locations.Error),
            Info = info
        });
    }

    private static string Language(string? language) =>
        OperationsSiteLanguages.IsSiteLanguage(language) ? language! : System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is var current && OperationsSiteLanguages.IsSiteLanguage(current) ? current : OperationsSiteLanguages.All[0];

    private static string Saved(HttpContext http, string what)
    {
        Microsoft.Extensions.Localization.IStringLocalizer<Vixly.Operations.Resources.OperationsText> text =
            http.RequestServices.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Vixly.Operations.Resources.OperationsText>>();
        return what switch
        {
            "published" => text["ui.editor.saved.published"],
            "image" => text["ui.editor.saved.image"],
            "location" => text["ui.editor.saved.location"],
            _ => text["ui.editor.saved.draft"]
        };
    }

    private static IResult Page<TComponent>(object parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        return new RazorComponentResult<TComponent>(parameters);
    }

    private static async Task<string?> CompanyAsync(HttpContext http, GetOperationsSignInBrandUseCase brand)
    {
        GetOperationsSignInBrandResponse response = await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted);
        return response.DisplayName;
    }
}

