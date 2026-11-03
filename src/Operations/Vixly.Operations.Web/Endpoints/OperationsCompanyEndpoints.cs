using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.UseCases.Company.CreateOperationsPayLink;
using Vixly.Operations.Application.UseCases.Company.GetOperationsBilling;
using Vixly.Operations.Application.UseCases.Company.GetOperationsBillingDocument;
using Vixly.Operations.Application.UseCases.Company.GetOperationsCompany;
using Vixly.Operations.Application.UseCases.Company.GetOperationsDocumentLogo;
using Vixly.Operations.Application.UseCases.Company.RemoveOperationsDocumentLogo;
using Vixly.Operations.Application.UseCases.Company.SaveOperationsDocumentLogo;
using Vixly.Operations.Application.UseCases.Company.SubmitOperationsCompanyChange;
using Vixly.Operations.Application.UseCases.Company.UpdateOperationsCompanyContacts;
using Vixly.Operations.Application.UseCases.Company.UpdateOperationsCompanyProfile;
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
// - /staff/company: the company profile (saved directly), contacts, language, time zone, and legal name change requests
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
            await CompanyPageAsync(http, get, brand, text, null, done is null ? null : texts["ui.company.done." + done switch
            {
                "change" => "change",
                "logo_saved" => "logo_saved",
                "logo_removed" => "logo_removed",
                _ => "saved"
            }].Value));

        // Owner, 2026-10-28: the logo on documents and labels, shown on the company page once saved.
        staff.MapGet("/company/document-logo", async (HttpContext http, bool? label, GetOperationsDocumentLogoUseCase get) =>
        {
            GetOperationsDocumentLogoResponse logo = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsDocumentLogoRequest { Label = label == true }, http.RequestAborted);

            if (logo.Content is not { } content)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "private, max-age=3600";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(content, logo.ContentType);
        });

        staff.MapPost("/company/document-logo", async (HttpContext http, IFormFile? logo, SaveOperationsDocumentLogoUseCase save, GetOperationsCompanyUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            byte[] content = Array.Empty<byte>();

            if (logo is { Length: > 0 and <= Vixly.Operations.Domain.Branding.OperationsSiteImage.MaxBytes })
            {
                await using MemoryStream buffer = new();
                await logo.CopyToAsync(buffer, http.RequestAborted);
                content = buffer.ToArray();
            }

            SaveOperationsDocumentLogoResponse response = logo is { Length: > Vixly.Operations.Domain.Branding.OperationsSiteImage.MaxBytes }
                ? SaveOperationsDocumentLogoResponse.Failure(Vixly.Operations.Domain.Branding.OperationsBrandingErrors.ImageTooLarge)
                : await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsDocumentLogoRequest { Content = content }, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/company?done=logo_saved#document-logo")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/company#document-logo") ?? await CompanyPageAsync(http, get, brand, text, response.Error, null);
        });

        staff.MapPost("/company/document-logo/remove", async (HttpContext http, RemoveOperationsDocumentLogoUseCase remove, GetOperationsCompanyUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            RemoveOperationsDocumentLogoResponse response = await remove.ExecuteAsync(OperationsCurrentUser.Context(http), new RemoveOperationsDocumentLogoRequest { Confirmed = true },
                http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/company?done=logo_removed#document-logo")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/company#document-logo") ?? await CompanyPageAsync(http, get, brand, text, response.Error, null);
        });

        // Sample PDFs drawn with the saved logo, to check it before a customer sees it.
        staff.MapGet("/company/document-logo/sample", async (HttpContext http, GetOperationsDocumentLogoUseCase get, GetOperationsSignInBrandUseCase brand, IOperationsLabelRenderer renderer) =>
        {
            if (!(await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsDocumentLogoRequest(), http.RequestAborted)).Succeeded)
            {
                return Results.NotFound();
            }

            string company = (await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted)).DisplayName ?? string.Empty;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            byte[] pdf = renderer.RenderReceipt(new OperationsReceiptDocument(company, "WH1", Text(http, "ui.company.document_logo.sample_warehouse"), "SAMPLE", "UPS",
                now.AddMinutes(-20), now, Text(http, "ui.company.document_logo.sample_person"), 1, 0,
                new[] { new OperationsReceiptLine("PK26000001", "1Z999AA10123456784", "C00001", false) }));
            return Pdf(http, pdf);
        });

        staff.MapGet("/company/document-logo/sample-label", async (HttpContext http, GetOperationsDocumentLogoUseCase get, GetOperationsSignInBrandUseCase brand, IOperationsLabelRenderer renderer) =>
        {
            if (!(await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsDocumentLogoRequest(), http.RequestAborted)).Succeeded)
            {
                return Results.NotFound();
            }

            string company = (await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted)).DisplayName ?? string.Empty;
            string tracking = Vixly.Operations.Domain.Shipments.OperationsTrackingNumbers.Make(Vixly.Operations.Domain.Shipments.OperationsTrackingNumbers.DefaultPrefix,
                new[] { 4, 8, 2, 9, 1, 7, 3, 6, 0, 5 });
            byte[] pdf = renderer.RenderShippingLabel(new OperationsShippingLabel(company, "SH26000001", "PK26000001", 1, 1, "FLL", "CAY", "air_standard", "Jean Baptiste", "Les Cayes", true,
                "4.5 lb", tracking, null, "VXA", "Air standard", "air", false, DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), "FLL", false, false));
            return Pdf(http, pdf);
        });

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

        // 2026-10-25: everything but the legal name is the company's own: saved at once, no approval.
        staff.MapPost("/company/profile", async (HttpContext http, [FromForm] string? tradingName, [FromForm] string? email, [FromForm] string? phone,
            [FromForm] string? line1, [FromForm] string? line2, [FromForm] string? city, [FromForm] string? region, [FromForm] string? postalCode, [FromForm] string? country,
            UpdateOperationsCompanyProfileUseCase update, GetOperationsCompanyUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            UpdateOperationsCompanyProfileResponse response = await Unavailable(() => update.ExecuteAsync(OperationsCurrentUser.Context(http), new UpdateOperationsCompanyProfileRequest
            {
                Input = new OperationsCompanyChangeInput
                {
                    TradingName = string.IsNullOrWhiteSpace(tradingName) ? null : tradingName.Trim(),
                    Email = email?.Trim() ?? string.Empty,
                    Phone = phone?.Trim() ?? string.Empty,
                    AddressLine1 = line1?.Trim() ?? string.Empty,
                    AddressLine2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim(),
                    City = city?.Trim() ?? string.Empty,
                    StateProvince = string.IsNullOrWhiteSpace(region) ? null : region.Trim(),
                    PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim(),
                    CountryCode = (country ?? string.Empty).Trim().ToUpperInvariant()
                }
            }, http.RequestAborted));

            return response.Succeeded
                ? Results.Redirect("/staff/company?done=saved")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/company") ?? await CompanyPageAsync(http, get, brand, text, response.Error, null);
        });

        // The legal name only, with a reason and proof; Vixly approves it (B12).
        staff.MapPost("/company/change", async (HttpContext http, [FromForm] string? legalName,
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
        GetOperationsDocumentLogoResponse logo = await http.RequestServices.GetRequiredService<GetOperationsDocumentLogoUseCase>()
            .ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsDocumentLogoRequest(), http.RequestAborted);
        http.Response.Headers.CacheControl = "no-store";

        return company.View is null
            ? new RazorComponentResult<EditorMessagePage>(new { Title = Text(http, "ui.company.title"), Company = name, Section = "company", Error = text.Describe(company.Error) })
            : new RazorComponentResult<CompanyPage>(new { Company = name, View = company.View, Error = text.Describe(error), Info = info, DocumentLogo = logo.Succeeded ? logo : null });
    }

    // Shown in the tab, ready to print; never kept by a cache.
    private static IResult Pdf(HttpContext http, byte[] pdf)
    {
        http.Response.Headers.CacheControl = "private, no-store";
        http.Response.Headers.XContentTypeOptions = "nosniff";
        return Results.File(pdf, "application/pdf");
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
