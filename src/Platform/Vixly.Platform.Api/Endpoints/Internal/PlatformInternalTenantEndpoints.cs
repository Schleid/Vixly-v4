using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vixly.Contracts.Platform.Internal.Billing;
using Vixly.Contracts.Platform.Internal.Legal;
using Vixly.Contracts.Platform.Internal.Subscription;
using Vixly.Contracts.Platform.Internal.Tenant;
using Vixly.Platform.Api.Internal;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal.AcceptPlatformLegalTerms;
using Vixly.Platform.Application.UseCases.Legal.GetPlatformTenantLegalStatus;
using Vixly.Platform.Application.UseCases.TenantAccount.ChangeTenantSubscription;
using Vixly.Platform.Application.UseCases.TenantAccount.CreateTenantPayLink;
using Vixly.Platform.Application.UseCases.TenantAccount.GetTenantAccount;
using Vixly.Platform.Application.UseCases.TenantAccount.GetTenantCompanyChange;
using Vixly.Platform.Application.UseCases.TenantAccount.SubmitTenantCompanyChange;
using Vixly.Platform.Application.UseCases.TenantAccount.WithdrawTenantCompanyChange;
using Vixly.Platform.Application.UseCases.TenantAccount.GetTenantBillingDocument;
using Vixly.Platform.Application.UseCases.TenantAccount.GetTenantEntitlements;
using Vixly.Platform.Application.UseCases.TenantAccount.GetTenantSubscription;
using Vixly.Platform.Application.UseCases.TenantAccount.ListTenantBillingDocuments;
using Vixly.Platform.Application.UseCases.TenantAccount.ListTenantInvoices;
using Vixly.Platform.Application.UseCases.TenantAccount.ListTenantPlanOptions;
using Vixly.Platform.Application.UseCases.TenantAccount.PreviewTenantSubscriptionChange;
using Vixly.Platform.Application.UseCases.TenantAccount.UpdateTenantAccount;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Api.Endpoints.Internal;

// =========================================================
// FILE: PlatformInternalTenantEndpoints.cs
//
// PURPOSE:
// The tenant's own account, entitlements, subscription, billing and legal calls for Operations (calls 12 to 23).
//
// RESPONSIBILITIES:
// - Map each call to its use case with the tenant context from the token
// - Mark reads cacheable for 5 minutes
//
// IMPORTANT NOTES:
// - The tenant is always the one in the token (S-13)
// =========================================================
public static class PlatformInternalTenantEndpoints
{
    public static void Map(RouteGroupBuilder tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        tenant.MapGet(string.Empty, async (GetTenantAccountUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            GetTenantAccountResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new GetTenantAccountRequest(), cancellationToken);
            return response.Succeeded ? PlatformInternalCache.Ok(http, PlatformInternalMapping.Account(response.Account!), PlatformInternalCache.TenantSeconds) : errors.InternalProblem(response.Error!);
        });

        tenant.MapPut(string.Empty, async (UpdatePlatformTenantAccountBody body, UpdateTenantAccountUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            List<PlatformContactInput>? contacts = null;

            if (body.Contacts is not null)
            {
                contacts = new List<PlatformContactInput>();

                foreach (PlatformTenantContactBody contact in body.Contacts)
                {
                    if (PlatformWireCodes.Parse<PlatformTenantContactRole>(contact.Role) is not { } role)
                    {
                        return errors.InternalProblem(PlatformCommonErrors.RequestInvalid);
                    }

                    contacts.Add(new PlatformContactInput { Role = role, Name = contact.Name ?? string.Empty, Email = contact.Email ?? string.Empty, Phone = contact.Phone });
                }
            }

            UpdateTenantAccountResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http),
                new UpdateTenantAccountRequest { Contacts = contacts, Language = body.Language, TimeZone = body.TimeZone }, cancellationToken);
            return response.Succeeded ? Results.NoContent() : errors.InternalProblem(response.Error!);
        });

        tenant.MapGet("/entitlements", async (GetTenantEntitlementsUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            GetTenantEntitlementsResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new GetTenantEntitlementsRequest(), cancellationToken);
            return response.Succeeded ? PlatformInternalCache.Ok(http, PlatformInternalMapping.Entitlements(response.Entitlements!), PlatformInternalCache.TenantSeconds) : errors.InternalProblem(response.Error!);
        });

        tenant.MapGet("/subscription", async (string? language, GetTenantSubscriptionUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            GetTenantSubscriptionResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new GetTenantSubscriptionRequest { Language = Language(language) }, cancellationToken);

            if (!response.Succeeded)
            {
                return errors.InternalProblem(response.Error!);
            }

            return response.Subscription is null
                ? Results.NoContent()
                : PlatformInternalCache.Ok(http, PlatformInternalMapping.Subscription(response.Subscription), PlatformInternalCache.TenantSeconds);
        });

        tenant.MapGet("/plans", async (string? language, ListTenantPlanOptionsUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            ListTenantPlanOptionsResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new ListTenantPlanOptionsRequest { Language = Language(language) }, cancellationToken);
            return response.Succeeded ? PlatformInternalCache.Ok(http, response.Options.Select(PlatformInternalMapping.Plan).ToArray(), PlatformInternalCache.TenantSeconds) : errors.InternalProblem(response.Error!);
        });

        tenant.MapPost("/subscription/preview", async (PlatformSubscriptionChangeBody body, PreviewTenantSubscriptionChangeUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            if (PlatformWireCodes.Parse<PlatformSubscriptionTerm>(body.Term) is not { } term)
            {
                return errors.InternalProblem(PlatformCommonErrors.RequestInvalid);
            }

            PreviewTenantSubscriptionChangeResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http),
                new PreviewTenantSubscriptionChangeRequest { PlatformPlanId = body.PlanId, Term = term, Licences = body.Licences }, cancellationToken);
            return response.Succeeded ? Results.Ok(PlatformInternalMapping.Quote(response.Quote!)) : errors.InternalProblem(response.Error!);
        });

        tenant.MapPost("/subscription", async (PlatformSubscriptionChangeBody body, ChangeTenantSubscriptionUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            if (PlatformWireCodes.Parse<PlatformSubscriptionTerm>(body.Term) is not { } term)
            {
                return errors.InternalProblem(PlatformCommonErrors.RequestInvalid);
            }

            ChangeTenantSubscriptionResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http),
                new ChangeTenantSubscriptionRequest { PlatformPlanId = body.PlanId, Term = term, Licences = body.Licences }, cancellationToken);
            return response.Succeeded ? Results.Ok(PlatformInternalMapping.Quote(response.Quote!)) : errors.InternalProblem(response.Error!);
        });

        tenant.MapGet("/invoices", async (int? page, int? pageSize, ListTenantInvoicesUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            ListTenantInvoicesResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http),
                new ListTenantInvoicesRequest { Page = page ?? 1, PageSize = pageSize ?? 25 }, cancellationToken);
            return response.Succeeded ? Results.Ok(PlatformInternalMapping.Page(response.Invoices!, PlatformInternalMapping.Invoice)) : errors.InternalProblem(response.Error!);
        });

        tenant.MapPost("/invoices/{invoiceId:guid}/pay-link", async (Guid invoiceId, CreateTenantPayLinkUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            CreateTenantPayLinkResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new CreateTenantPayLinkRequest { PlatformInvoiceId = invoiceId }, cancellationToken);
            return response.Succeeded ? Results.Ok(new PlatformPayLinkInfo(response.Url, response.ExpiresUtc)) : errors.InternalProblem(response.Error!);
        });

        tenant.MapGet("/documents", async (int? page, int? pageSize, ListTenantBillingDocumentsUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            ListTenantBillingDocumentsResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http),
                new ListTenantBillingDocumentsRequest { Page = page ?? 1, PageSize = pageSize ?? 25 }, cancellationToken);
            return response.Succeeded ? Results.Ok(PlatformInternalMapping.Page(response.Documents!, PlatformInternalMapping.Document)) : errors.InternalProblem(response.Error!);
        });

        tenant.MapGet("/documents/{kind}/{documentId:guid}", async (string kind, Guid documentId, GetTenantBillingDocumentUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            if (PlatformWireCodes.Parse<PlatformBillingDocumentKind>(kind) is not { } documentKind)
            {
                return errors.InternalProblem(PlatformBillingErrors.DocumentNotFound);
            }

            GetTenantBillingDocumentResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http),
                new GetTenantBillingDocumentRequest { Kind = documentKind, OwnerId = documentId }, cancellationToken);

            if (!response.Succeeded)
            {
                return errors.InternalProblem(response.Error!);
            }

            http.Response.Headers.CacheControl = "private, max-age=3600";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(response.Content!, "application/pdf", response.FileName);
        });

        // B12: the tenant owner asks TechCrafters to change the company's legal details.
        tenant.MapGet("/company-change", async (GetTenantCompanyChangeUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            GetTenantCompanyChangeResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new GetTenantCompanyChangeRequest(), cancellationToken);

            if (!response.Succeeded)
            {
                return errors.InternalProblem(response.Error!);
            }

            http.Response.Headers.CacheControl = "no-store";
            return response.Change is { } change ? Results.Ok(PlatformInternalMapping.CompanyChange(change)) : Results.NoContent();
        });

        tenant.MapPost("/company-change", async (PlatformCompanyChangeBody body, SubmitTenantCompanyChangeUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            byte[]? proof = null;

            if (!string.IsNullOrWhiteSpace(body.ProofBase64))
            {
                proof = new byte[body.ProofBase64.Length * 3 / 4 + 3];

                if (!Convert.TryFromBase64String(body.ProofBase64, proof, out int written))
                {
                    return errors.InternalProblem(PlatformCommonErrors.RequestInvalid);
                }

                proof = proof[..written];
            }

            SubmitTenantCompanyChangeResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new SubmitTenantCompanyChangeRequest
            {
                Company = new PlatformCompanyInput
                {
                    LegalName = body.LegalName,
                    TradingName = body.TradingName,
                    Email = body.Email,
                    Phone = body.Phone,
                    AddressLine1 = body.AddressLine1,
                    AddressLine2 = body.AddressLine2,
                    City = body.City,
                    StateProvince = body.StateProvince,
                    PostalCode = body.PostalCode,
                    CountryCode = body.CountryCode
                },
                Reason = body.Reason,
                ProofFileName = body.ProofFileName,
                ProofContent = proof
            }, cancellationToken);

            return response.Succeeded
                ? Results.Created("/internal/v1/tenant/company-change", new PlatformCompanyChangeReceipt(response.RequestId))
                : errors.InternalProblem(response.Error!);
        });

        tenant.MapDelete("/company-change", async (WithdrawTenantCompanyChangeUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            WithdrawTenantCompanyChangeResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new WithdrawTenantCompanyChangeRequest(), cancellationToken);
            return response.Succeeded ? Results.NoContent() : errors.InternalProblem(response.Error!);
        });

        tenant.MapGet("/legal", async (string? language, GetPlatformTenantLegalStatusUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            GetPlatformTenantLegalStatusResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http), new GetPlatformTenantLegalStatusRequest { Language = Language(language) }, cancellationToken);
            return response.Succeeded ? PlatformInternalCache.Ok(http, PlatformInternalMapping.Legal(response.Answer!), PlatformInternalCache.TenantSeconds) : errors.InternalProblem(response.Error!);
        });

        tenant.MapPost("/legal/accept", async (AcceptPlatformLegalTermsBody body, AcceptPlatformLegalTermsUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            AcceptPlatformLegalTermsResponse response = await useCase.ExecuteAsync(PlatformInternalRequest.Tenant(http),
                new AcceptPlatformLegalTermsRequest { DocumentId = body.DocumentId, Sha256 = body.Sha256, Language = body.Language }, cancellationToken);
            return response.Succeeded ? Results.NoContent() : errors.InternalProblem(response.Error!);
        });
    }

    // The query language first, then the caller's Accept-Language (set by request localization).
    private static string Language(string? language)
    {
        return string.IsNullOrWhiteSpace(language) ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName : language.Trim();
    }
}
