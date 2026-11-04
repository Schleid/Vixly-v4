using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.UseCases.Insurance.ArchiveOperationsInsurancePolicy;
using Vixly.Operations.Application.UseCases.Insurance.AttachOperationsPolicyDocument;
using Vixly.Operations.Application.UseCases.Insurance.GetOperationsInsuranceCertificate;
using Vixly.Operations.Application.UseCases.Insurance.GetOperationsInsurancePolicy;
using Vixly.Operations.Application.UseCases.Insurance.GetOperationsPolicyDocument;
using Vixly.Operations.Application.UseCases.Insurance.LinkOperationsCoverPolicy;
using Vixly.Operations.Application.UseCases.Insurance.ListOperationsInsurance;
using Vixly.Operations.Application.UseCases.Insurance.RemoveOperationsPolicyDocument;
using Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsurancePolicy;
using Vixly.Operations.Application.UseCases.Insurance.SaveOperationsLiabilityTerms;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Insurance;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsInsuranceEndpoints.cs
//
// PURPOSE:
// The insurance module for staff (Slice 5B step 1, design sections 3 and 4).
//
// RESPONSIBILITIES:
// - /staff/insurance: the company's policies (ending soon first), the cover sold per mode and the policy behind it, the liability terms
// - /staff/insurance/policies/new and /{id}: record or change a policy, its documents, archive it
// - /staff/shipments/{id}/certificate: a shipment's insurance certificate (PDF)
//
// IMPORTANT NOTES:
// - Forms with antiforgery and the form guard (ADR-0019); the use cases check the permissions
// - Documents are served as sandboxed downloads
// =========================================================
public static class OperationsInsuranceEndpoints
{
    private const int MaxDocumentBytes = 10 * 1024 * 1024;

    public static IEndpointRouteBuilder MapOperationsInsuranceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff/insurance").RequireAuthorization().RequireFormToken();

        staff.MapGet("/", async (HttpContext http, string? done, ListOperationsInsuranceUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) =>
            await OverviewAsync(http, null, Done(text, done), list, brand, errors));

        staff.MapPost("/covers/{mode}", async (HttpContext http, string mode, [FromForm] string? policy, [FromForm] Guid? plan, LinkOperationsCoverPolicyUseCase link, ListOperationsInsuranceUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            LinkOperationsCoverPolicyResponse response = await link.ExecuteAsync(OperationsCurrentUser.Context(http), new LinkOperationsCoverPolicyRequest
            {
                Mode = Mode(mode), PlanId = plan, PolicyId = string.IsNullOrEmpty(policy) ? null : Guid.TryParse(policy, out Guid id) ? id : Guid.Empty
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/insurance?done=cover") : await OverviewAsync(http, response.Error, null, list, brand, errors);
        });

        // Slice 5D (D-6): whether staff may change the insurance of an open shipment; the checkbox wins over the hidden "false" when ticked.
        staff.MapPost("/settings", async (HttpContext http, [FromForm(Name = "staff_may_change")] string? staffMayChange,
            Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsuranceSettings.SaveOperationsInsuranceSettingsUseCase save, ListOperationsInsuranceUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsuranceSettings.SaveOperationsInsuranceSettingsResponse response = await save.ExecuteAsync(
                OperationsCurrentUser.Context(http),
                new Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsuranceSettings.SaveOperationsInsuranceSettingsRequest
                {
                    StaffMayChangeInsurance = http.Request.Form["staff_may_change"].Contains("true")
                }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/insurance?done=settings") : await OverviewAsync(http, response.Error, null, list, brand, errors);
        });

        staff.MapPost("/liability/{mode}", async (HttpContext http, string mode, [FromForm] string? basis, [FromForm] string? amount, [FromForm] string? cap,
            [FromForm(Name = "customer_text")] string? customerText, SaveOperationsLiabilityTermsUseCase save, ListOperationsInsuranceUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            if (!OperationsChargesEndpoints.Choice(basis, out OperationsLiabilityBasis chosen) || !OperationsChargesEndpoints.Amounts(out decimal?[] amounts, amount, cap))
            {
                return await OverviewAsync(http, OperationsInsuranceErrors.LiabilityInvalid, null, list, brand, errors);
            }

            SaveOperationsLiabilityTermsResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsLiabilityTermsRequest
            {
                Mode = Mode(mode), Basis = chosen, Amount = amounts[0], Cap = amounts[1], CustomerText = customerText
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/insurance?done=liability") : await OverviewAsync(http, response.Error, null, list, brand, errors);
        });

        staff.MapGet("/policies/new", async (HttpContext http, GetOperationsSignInBrandUseCase brand, ListOperationsInsuranceUseCase list) =>
        {
            ListOperationsInsuranceResponse access = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsInsuranceRequest(), http.RequestAborted);
            return access.CanManage
                ? OperationsStaffResults.Page<PolicyPage>(http, new { Company = await OperationsStaffResults.CompanyAsync(http, brand), CanManage = true, Currency = access.Settings?.CurrencyCode })
                : Results.NotFound();
        });

        staff.MapPost("/policies/new", async (HttpContext http, SaveOperationsInsurancePolicyUseCase save, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await SaveAsync(http, null, save, brand, errors));

        staff.MapGet("/policies/{policyId:guid}", async (HttpContext http, Guid policyId, string? done, GetOperationsInsurancePolicyUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await PolicyAsync(http, policyId, null, Done(text, done), null, get, brand, errors));

        staff.MapPost("/policies/{policyId:guid}", async (HttpContext http, Guid policyId, SaveOperationsInsurancePolicyUseCase save, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
            await SaveAsync(http, policyId, save, brand, errors));

        staff.MapPost("/policies/{policyId:guid}/archive", async (HttpContext http, Guid policyId, [FromForm] bool? archived, ArchiveOperationsInsurancePolicyUseCase archive,
            GetOperationsInsurancePolicyUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ArchiveOperationsInsurancePolicyResponse response = await archive.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ArchiveOperationsInsurancePolicyRequest { PolicyId = policyId, Archived = archived != false }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/insurance/policies/" + policyId + "?done=" + (archived != false ? "archived" : "restored"))
                : await PolicyAsync(http, policyId, response.Error, null, null, get, brand, errors);
        });

        staff.MapPost("/policies/{policyId:guid}/documents", async (HttpContext http, Guid policyId, AttachOperationsPolicyDocumentUseCase attach, GetOperationsInsurancePolicyUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form;

            // A file above the form limit fails while the form is read: staff see the size rule, not an error page.
            try
            {
                form = await http.Request.ReadFormAsync(http.RequestAborted);
            }
            catch (Exception exception) when (exception is InvalidDataException or BadHttpRequestException)
            {
                return await PolicyAsync(http, policyId, OperationsInsuranceErrors.DocumentInvalid, null, null, get, brand, errors);
            }

            if (form.Files.GetFile("document") is not { Length: > 0 and <= MaxDocumentBytes } file)
            {
                return await PolicyAsync(http, policyId, OperationsInsuranceErrors.DocumentInvalid, null, null, get, brand, errors);
            }

            using MemoryStream buffer = new((int)file.Length);
            await file.CopyToAsync(buffer, http.RequestAborted);
            AttachOperationsPolicyDocumentResponse response = await attach.ExecuteAsync(OperationsCurrentUser.Context(http), new AttachOperationsPolicyDocumentRequest
            {
                PolicyId = policyId, Kind = form["kind"].ToString(), FileName = file.FileName, Content = buffer.ToArray()
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/insurance/policies/" + policyId + "?done=document")
                : await PolicyAsync(http, policyId, response.Error, null, null, get, brand, errors);
        }).WithFormOptions(multipartBodyLengthLimit: MaxDocumentBytes + (1024 * 1024));

        staff.MapPost("/policies/{policyId:guid}/documents/{documentId:guid}/remove", async (HttpContext http, Guid policyId, Guid documentId, RemoveOperationsPolicyDocumentUseCase remove,
            GetOperationsInsurancePolicyUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            RemoveOperationsPolicyDocumentResponse response = await remove.ExecuteAsync(OperationsCurrentUser.Context(http),
                new RemoveOperationsPolicyDocumentRequest { PolicyId = policyId, DocumentId = documentId }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/insurance/policies/" + policyId + "?done=document_removed")
                : await PolicyAsync(http, policyId, response.Error, null, null, get, brand, errors);
        });

        staff.MapGet("/policies/{policyId:guid}/documents/{documentId:guid}", async (HttpContext http, Guid policyId, Guid documentId, GetOperationsPolicyDocumentUseCase get) =>
        {
            GetOperationsPolicyDocumentResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http),
                new GetOperationsPolicyDocumentRequest { PolicyId = policyId, DocumentId = documentId }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            return Results.File(response.Content, response.ContentType, response.FileName);
        });

        endpoints.MapGet("/staff/shipments/{shipmentId:guid}/certificate", async (HttpContext http, Guid shipmentId, GetOperationsInsuranceCertificateUseCase get) =>
        {
            GetOperationsInsuranceCertificateResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http),
                new GetOperationsInsuranceCertificateRequest { ShipmentId = shipmentId }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(response.Content, "application/pdf", response.FileName);
        }).RequireAuthorization();

        return endpoints;
    }

    private static string? Done(IStringLocalizer<OperationsText> text, string? done) =>
        done is "saved" or "cover" or "settings" or "liability" or "archived" or "restored" or "document" or "document_removed" ? text["ui.insurance_module.done." + done].Value : null;

    private static OperationsTransportMode Mode(string? value) =>
        Enum.TryParse(value, true, out OperationsTransportMode mode) && Enum.IsDefined(mode) && !value!.Any(char.IsDigit) ? mode : 0;

    private static async Task<IResult> OverviewAsync(HttpContext http, Error? error, string? info, ListOperationsInsuranceUseCase list, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        ListOperationsInsuranceResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsInsuranceRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<InsurancePage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Policies = response.Policies,
            Covers = response.Covers,
            Liability = response.Liability,
            Today = response.Today,
            CanManage = response.CanManage,
            Settings = response.Settings,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> PolicyAsync(HttpContext http, Guid policyId, Error? error, string? info, OperationsPolicyTerms? entered, GetOperationsInsurancePolicyUseCase get,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsInsurancePolicyResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsInsurancePolicyRequest { PolicyId = policyId }, http.RequestAborted);

        if (response.Policy is not { } policy)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<PolicyPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Policy = policy,
            Entered = entered,
            Covers = response.Covers,
            Today = response.Today,
            CanManage = response.CanManage,
            Currency = policy.CurrencyCode,
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static async Task<IResult> SaveAsync(HttpContext http, Guid? policyId, SaveOperationsInsurancePolicyUseCase save, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
        OperationsPolicyTerms? terms = Read(form);
        Error? error = terms is null ? OperationsInsuranceErrors.DetailsInvalid : null;

        if (terms is not null)
        {
            SaveOperationsInsurancePolicyResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http),
                new SaveOperationsInsurancePolicyRequest { PolicyId = policyId, Terms = terms }, http.RequestAborted);

            if (response.Succeeded)
            {
                return Results.Redirect("/staff/insurance/policies/" + response.PolicyId + "?done=saved");
            }

            error = response.Error;
        }

        // Shown again with what was typed, and the reason.
        if (policyId is { } id)
        {
            return await PolicyAsync(http, id, error, null, terms, http.RequestServices.GetRequiredService<GetOperationsInsurancePolicyUseCase>(), brand, errors);
        }

        return OperationsStaffResults.Page<PolicyPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Entered = terms,
            CanManage = true,
            Currency = form["currency"].ToString(),
            Error = errors.Describe(error)
        });
    }

    // The form's fields; null when a number or date cannot be read.
    private static OperationsPolicyTerms? Read(IFormCollection form)
    {
        if (!OperationsChargesEndpoints.Choice(form["scope"], out OperationsPolicyScope scope) || !OperationsChargesEndpoints.Choice(form["kind"], out OperationsPolicyKind kind)
            || !OperationsChargesEndpoints.Amounts(out decimal?[] amounts, form["limit_shipment"], form["limit_conveyance"], form["limit_location"], form["limit_year"],
                form["deductible_amount"], form["deductible_percent"], form["deductible_minimum"])
            || !Day(form["starts"], out DateOnly? starts) || !Day(form["ends"], out DateOnly? ends))
        {
            return null;
        }

        int? notice = int.TryParse(form["notice_days"], NumberStyles.None, CultureInfo.InvariantCulture, out int days) ? days : null;

        if (!string.IsNullOrWhiteSpace(form["notice_days"]) && notice is null)
        {
            return null;
        }

        OperationsChargesEndpoints.Choice(form["declarations"], out OperationsDeclarationFrequency declarations);
        return new OperationsPolicyTerms(scope, kind, form["insurer"], form["insurer_email"], form["insurer_phone"], form["claims_email"], form["claims_phone"],
            form["broker"], form["broker_email"], form["broker_phone"], form["number"], starts, ends, form["currency"], amounts[0], amounts[1], amounts[2], amounts[3],
            amounts[4], amounts[5], amounts[6], Flags<OperationsPolicyRisks>(form["risks"]), Flags<OperationsPolicyModes>(form["modes"]), form["territory"],
            Flags<OperationsInsuranceExclusions>(form["exclusions"]), form["other_exclusions"], notice, declarations, form["notes"]);
    }

    private static T Flags<T>(Microsoft.Extensions.Primitives.StringValues values)
        where T : struct, Enum =>
        (T)Enum.ToObject(typeof(T), values.Select(value => OperationsChargesEndpoints.Choice(value, out T flag) ? Convert.ToInt64(flag, CultureInfo.InvariantCulture) : 0L)
            .Aggregate(0L, (all, flag) => all | flag));

    private static bool Day(string? value, out DateOnly? day)
    {
        day = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        bool read = DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed);
        day = read ? parsed : null;
        return read;
    }
}
