using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.UseCases.Company.CreateOperationsPayLink;
using Vixly.Operations.Application.UseCases.Company.GetOperationsBilling;
using Vixly.Operations.Application.UseCases.Company.GetOperationsBillingDocument;
using Vixly.Operations.Application.UseCases.Company.GetOperationsCompany;
using Vixly.Operations.Application.UseCases.Company.SubmitOperationsCompanyChange;
using Vixly.Operations.Application.UseCases.Company.UpdateOperationsCompanyContacts;
using Vixly.Operations.Application.UseCases.Company.WithdrawOperationsCompanyChange;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Editor;
using Vixly.Operations.Web.Components.Settings;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsCompanyEndpoints.cs
//
// PURPOSE:
// The company and billing pages (Slice 1E).
//
// RESPONSIBILITIES:
// - /staff/company: legal details, contacts, language, time zone and change requests
// - /staff/billing: the plan, invoices, pay links and billing PDFs
//
// IMPORTANT NOTES:
// - Static forms with antiforgery (ADR-0019); Platform checks the role and step-up again
// - A pay link goes only to an https address Platform gave
// =========================================================
public static class OperationsCompanyEndpoints
{
    private const long ProofMaxBytes = 10 * 1024 * 1024;

    public static IEndpointRouteBuilder MapOperationsCompanyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Every change under /staff checks the antiforgery token, also when the form carries no other field (ADR-0019).
        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/company", async (HttpContext http, string? done, GetOperationsCompanyUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText text, IStringLocalizer<OperationsText> texts) =>
            await CompanyPageAsync(http, get, brand, text, null, done is null ? null : texts["ui.company.done." + (done == "change" ? "change" : "saved")].Value));

        staff.MapPost("/company/contacts", async (HttpContext http, [FromForm] string? billingName, [FromForm] string? billingEmail, [FromForm] string? billingPhone,
            [FromForm] string? technicalName, [FromForm] string? technicalEmail, [FromForm] string? technicalPhone, [FromForm] string? language, [FromForm] string? timeZone,
            UpdateOperationsCompanyContactsUseCase update, GetOperationsCompanyUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            List<OperationsCompanyContact> contacts = new();
            AddContact(contacts, "billing", billingName, billingEmail, billingPhone);
            AddContact(contacts, "technical", technicalName, technicalEmail, technicalPhone);

            UpdateOperationsCompanyContactsResponse response = await Unavailable(() => update.ExecuteAsync(OperationsCurrentUser.Context(http),
                new UpdateOperationsCompanyContactsRequest { Contacts = contacts, Language = language ?? "en", TimeZone = timeZone ?? string.Empty }, http.RequestAborted));

            return response.Succeeded
                ? Results.Redirect("/staff/company?done=saved")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/company") ?? await CompanyPageAsync(http, get, brand, text, response.Error, null);
        });

        staff.MapPost("/company/change", async (HttpContext http, [FromForm] string? legalName, [FromForm] string? tradingName, [FromForm] string? email, [FromForm] string? phone,
            [FromForm] string? line1, [FromForm] string? line2, [FromForm] string? city, [FromForm] string? region, [FromForm] string? postalCode, [FromForm] string? country,
            [FromForm] string? reason, IFormFile? proof, SubmitOperationsCompanyChangeUseCase submit, GetOperationsCompanyUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            byte[]? content = null;

            if (proof is { Length: > 0 and <= ProofMaxBytes })
            {
                await using MemoryStream buffer = new();
                await proof.CopyToAsync(buffer, http.RequestAborted);
                content = buffer.ToArray();
            }

            SubmitOperationsCompanyChangeResponse response = proof is { Length: > ProofMaxBytes }
                ? SubmitOperationsCompanyChangeResponse.Failure(Vixly.Operations.Application.Company.OperationsCompanyErrors.ProofTooLarge)
                : await Unavailable(() => submit.ExecuteAsync(OperationsCurrentUser.Context(http), new SubmitOperationsCompanyChangeRequest
                {
                    Input = new OperationsCompanyChangeInput
                    {
                        LegalName = legalName ?? string.Empty,
                        TradingName = tradingName,
                        Email = email ?? string.Empty,
                        Phone = phone ?? string.Empty,
                        AddressLine1 = line1 ?? string.Empty,
                        AddressLine2 = line2,
                        City = city ?? string.Empty,
                        StateProvince = region,
                        PostalCode = postalCode,
                        CountryCode = (country ?? string.Empty).Trim().ToUpperInvariant(),
                        Reason = reason ?? string.Empty,
                        ProofFileName = proof is null ? null : Path.GetFileName(proof.FileName),
                        ProofContent = content
                    }
                }, http.RequestAborted));

            return response.Succeeded
                ? Results.Redirect("/staff/company?done=change")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/company") ?? await CompanyPageAsync(http, get, brand, text, response.Error, null);
        });

        staff.MapPost("/company/change/withdraw", async (HttpContext http, WithdrawOperationsCompanyChangeUseCase withdraw, GetOperationsCompanyUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            WithdrawOperationsCompanyChangeResponse response = await Unavailable(() => withdraw.ExecuteAsync(OperationsCurrentUser.Context(http), new WithdrawOperationsCompanyChangeRequest(), http.RequestAborted));
            return response.Succeeded ? Results.Redirect("/staff/company?done=saved") : await CompanyPageAsync(http, get, brand, text, response.Error, null);
        });

        staff.MapGet("/billing", async (HttpContext http, int? page, GetOperationsBillingUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
            await BillingPageAsync(http, get, brand, text, page ?? 1, null));

        staff.MapPost("/billing/invoices/{invoiceId:guid}/pay", async (HttpContext http, Guid invoiceId, CreateOperationsPayLinkUseCase pay, GetOperationsBillingUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            CreateOperationsPayLinkResponse response = await Unavailable(() => pay.ExecuteAsync(OperationsCurrentUser.Context(http), new CreateOperationsPayLinkRequest { InvoiceId = invoiceId }, http.RequestAborted));
            return response.Succeeded ? Results.Redirect(response.Url) : await BillingPageAsync(http, get, brand, text, 1, response.Error);
        });

        staff.MapGet("/billing/documents/{kind}/{documentId:guid}", async (HttpContext http, string kind, Guid documentId, GetOperationsBillingDocumentUseCase get) =>
        {
            if (kind.Length > 40 || !kind.All(character => char.IsAsciiLetterLower(character) || character == '_'))
            {
                return Results.NotFound();
            }

            GetOperationsBillingDocumentResponse response = await Unavailable(() => get.ExecuteAsync(OperationsCurrentUser.Context(http),
                new GetOperationsBillingDocumentRequest { Kind = kind, DocumentId = documentId }, http.RequestAborted));
            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return response.File is { } file ? Results.File(file.Content, "application/pdf", kind + "-" + documentId.ToString("N")[..8] + ".pdf") : Results.NotFound();
        });

        return endpoints;
    }

    private static void AddContact(List<OperationsCompanyContact> contacts, string role, string? name, string? email, string? phone)
    {
        if (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(email))
        {
            contacts.Add(new OperationsCompanyContact(role, name?.Trim() ?? string.Empty, email?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()));
        }
    }

    private static async Task<IResult> CompanyPageAsync(HttpContext http, GetOperationsCompanyUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText text, Error? error, string? info)
    {
        GetOperationsCompanyResponse company = await Unavailable(() => get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCompanyRequest(), http.RequestAborted));
        string? name = (await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted)).DisplayName;
        http.Response.Headers.CacheControl = "no-store";

        return company.View is null
            ? new RazorComponentResult<EditorMessagePage>(new { Title = Text(http, "ui.company.title"), Company = name, Section = "company", Error = text.Describe(company.Error) })
            : new RazorComponentResult<CompanyPage>(new { Company = name, View = company.View, Error = text.Describe(error), Info = info });
    }

    private static async Task<IResult> BillingPageAsync(HttpContext http, GetOperationsBillingUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText text, int page, Error? error)
    {
        string language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        GetOperationsBillingResponse billing = await Unavailable(() => get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsBillingRequest { Language = language, Page = Math.Max(page, 1) }, http.RequestAborted));
        string? name = (await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted)).DisplayName;
        http.Response.Headers.CacheControl = "no-store";

        return billing.View is null
            ? new RazorComponentResult<EditorMessagePage>(new { Title = Text(http, "ui.billing.title"), Company = name, Section = "billing", Error = text.Describe(billing.Error) })
            : new RazorComponentResult<BillingPage>(new { Company = name, View = billing.View, Error = text.Describe(error) });
    }

    private static string Text(HttpContext http, string key) => http.RequestServices.GetRequiredService<IStringLocalizer<OperationsText>>()[key].Value;

    private static async Task<T> Unavailable<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (OperationsPlatformUnavailableException exception)
        {
            throw new BadHttpRequestException("Platform is not available.", StatusCodes.Status503ServiceUnavailable, exception);
        }
    }
}
