using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.UseCases.Customers.ChangeOperationsCustomerStatus;
using Vixly.Operations.Application.UseCases.Customers.GetOperationsCustomer;
using Vixly.Operations.Application.UseCases.Customers.ListOperationsCustomers;
using Vixly.Operations.Application.UseCases.Customers.SetOperationsCustomerCredit;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Customers;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsCustomerEndpoints.cs
//
// PURPOSE:
// The staff customer pages (Slice 2, design section 3).
//
// RESPONSIBILITIES:
// - /staff/customers: search and list
// - /staff/customers/{id}: details, the packages coming to the customer, credit
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); the use cases check permissions and step-up
// - A status change ends the customer's sessions (the use case does it)
// - Staff never enter or change a customer's details: they come from the customer's own registration (owner, 2026-10-18)
// =========================================================
public static class OperationsCustomerEndpoints
{
    public static IEndpointRouteBuilder MapOperationsCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Every change under /staff checks the antiforgery token, also when the form carries no other field (ADR-0019).
        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/customers", async (HttpContext http, string? search, int? page, ListOperationsCustomersUseCase list, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
            await ListAsync(http, search, Math.Max(page ?? 1, 1), null, null, list, brand, errors));

        staff.MapGet("/customers/{customerId:guid}", async (HttpContext http, Guid customerId, string? done, GetOperationsCustomerUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await CustomerAsync(http, customerId, null, Done(text, done), get, brand, errors));

        staff.MapPost("/customers/{customerId:guid}/status", async (HttpContext http, Guid customerId, [FromForm] string? status, [FromForm] string? reason,
            ChangeOperationsCustomerStatusUseCase useCase, GetOperationsCustomerUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsCustomerStatus? chosen = status switch
            {
                "active" => OperationsCustomerStatus.Active,
                "suspended" => OperationsCustomerStatus.Suspended,
                "closed" => OperationsCustomerStatus.Closed,
                _ => null
            };

            if (chosen is null)
            {
                return await CustomerAsync(http, customerId, OperationsCustomerErrors.StatusChangeInvalid, null, get, brand, errors);
            }

            ChangeOperationsCustomerStatusResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ChangeOperationsCustomerStatusRequest { CustomerId = customerId, Status = chosen.Value, Reason = reason ?? string.Empty }, http.RequestAborted);

            return await AfterAsync(http, customerId, response.Succeeded, response.Error, "status", get, brand, errors);
        });

        staff.MapPost("/customers/{customerId:guid}/credit", async (HttpContext http, Guid customerId, [FromForm] bool allowed, [FromForm] string? reason,
            SetOperationsCustomerCreditUseCase useCase, GetOperationsCustomerUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            SetOperationsCustomerCreditResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new SetOperationsCustomerCreditRequest { CustomerId = customerId, Allowed = allowed, Reason = reason ?? string.Empty }, http.RequestAborted);

            return await AfterAsync(http, customerId, response.Succeeded, response.Error, "credit", get, brand, errors);
        });

        return endpoints;
    }

    private static string? Done(IStringLocalizer<OperationsText> text, string? done) =>
        done is "status" or "credit" ? text["ui.customers.done." + done].Value : null;

    private static async Task<IResult> AfterAsync(HttpContext http, Guid customerId, bool succeeded, Error? error, string done, GetOperationsCustomerUseCase get,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        if (succeeded)
        {
            return Results.Redirect("/staff/customers/" + customerId + "?done=" + done);
        }

        return OperationsStaffEndpoints.StepUp(error, "/staff/customers/" + customerId) ?? await CustomerAsync(http, customerId, error, null, get, brand, errors);
    }

    private static async Task<IResult> ListAsync(HttpContext http, string? search, int page, Error? error, string? info, ListOperationsCustomersUseCase list,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        OperationsAuthorizationContext context = OperationsCurrentUser.Context(http);
        ListOperationsCustomersResponse response = await list.ExecuteAsync(context, new ListOperationsCustomersRequest { Search = search, Page = page }, http.RequestAborted);

        return Page<CustomerListPage>(http, new
        {
            Company = await CompanyAsync(http, brand),
            Search = search,
            Page = page,
            Customers = response.Customers,
            TotalCount = response.TotalCount,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> CustomerAsync(HttpContext http, Guid customerId, Error? error, string? info, GetOperationsCustomerUseCase get,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsCustomerResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCustomerRequest { CustomerId = customerId }, http.RequestAborted);

        if (response.Customer is not { } customer)
        {
            return response.Error?.Code == OperationsAccessErrors.PermissionRequired.Code
                ? Page<CustomerListPage>(http, new { Company = await CompanyAsync(http, brand), Error = errors.Describe(response.Error) })
                : Results.NotFound();
        }

        return Page<CustomerPage>(http, new
        {
            Company = await CompanyAsync(http, brand),
            Customer = customer,
            Incoming = response.Incoming,
            CanSeePackages = response.CanSeePackages,
            CanSetCredit = response.CanSetCredit,
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static IResult Page<TComponent>(HttpContext http, object parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        http.Response.Headers.CacheControl = "no-store";
        return new RazorComponentResult<TComponent>(parameters);
    }

    private static async Task<string?> CompanyAsync(HttpContext http, GetOperationsSignInBrandUseCase brand) =>
        (await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted)).DisplayName;
}
