using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.UseCases.Customers.ChangeOperationsCustomerStatus;
using Vixly.Operations.Application.UseCases.Customers.GetOperationsCustomer;
using Vixly.Operations.Application.UseCases.Customers.ListOperationsCustomers;
using Vixly.Operations.Application.UseCases.Customers.LookUpOperationsCustomers;
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

        staff.MapGet("/customers", async (HttpContext http, string? search, int? page, ListOperationsCustomersUseCase list, LookUpOperationsCustomersUseCase lookUp,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await ListOrLookUpAsync(http, search, Math.Max(page ?? 1, 1), list, lookUp, brand, errors));

        // Vixly Lite (owner, 2026-10-30): without the customer portal, staff enter customers.
        staff.MapGet("/customers/new", async (HttpContext http, GetOperationsSignInBrandUseCase brand) =>
            Page<CustomerNewPage>(http, new { Company = await CompanyAsync(http, brand), Form = new Vixly.Operations.Application.UseCases.Customers.CreateOperationsCustomer.CreateOperationsCustomerRequest() }));

        staff.MapPost("/customers/new", async (HttpContext http, [FromForm] string? firstName, [FromForm] string? lastName, [FromForm] string? companyName, [FromForm] string? email,
            [FromForm] string? phone, [FromForm] string? language, Vixly.Operations.Application.UseCases.Customers.CreateOperationsCustomer.CreateOperationsCustomerUseCase create,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            Vixly.Operations.Application.UseCases.Customers.CreateOperationsCustomer.CreateOperationsCustomerRequest form = new()
            {
                FirstName = firstName ?? string.Empty,
                LastName = lastName ?? string.Empty,
                CompanyName = string.IsNullOrWhiteSpace(companyName) ? null : companyName,
                Email = email ?? string.Empty,
                Phone = phone ?? string.Empty,
                Language = language ?? "en"
            };
            Vixly.Operations.Application.UseCases.Customers.CreateOperationsCustomer.CreateOperationsCustomerResponse response =
                await create.ExecuteAsync(OperationsCurrentUser.Context(http), form, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/customers/" + response.CustomerId)
                : Page<CustomerNewPage>(http, new { Company = await CompanyAsync(http, brand), Form = form, Error = errors.Describe(response.Error) });
        });

        staff.MapGet("/customers/{customerId:guid}", async (HttpContext http, Guid customerId, string? done, GetOperationsCustomerUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await CustomerAsync(http, customerId, null, Done(text, done), get, brand, errors));

        staff.MapPost("/customers/{customerId:guid}/status", async (HttpContext http, Guid customerId, [FromForm] string? status, [FromForm] string? reason, [FromForm] string? until,
            ChangeOperationsCustomerStatusUseCase useCase, GetOperationsCustomerUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsCustomerStatus? chosen = status switch
            {
                "active" => OperationsCustomerStatus.Active,
                "suspended" => OperationsCustomerStatus.Suspended,
                "closed" => OperationsCustomerStatus.Closed,
                "banned" => OperationsCustomerStatus.Banned,
                _ => null
            };

            if (chosen is null)
            {
                return await CustomerAsync(http, customerId, OperationsCustomerErrors.StatusChangeInvalid, null, get, brand, errors);
            }

            ChangeOperationsCustomerStatusResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ChangeOperationsCustomerStatusRequest
                {
                    CustomerId = customerId, Status = chosen.Value, Reason = reason ?? string.Empty, PortalSlug = OperationsStaffResults.PortalSlug(http),
                    SuspendedUntil = chosen == OperationsCustomerStatus.Suspended && DateOnly.TryParseExact(until, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateOnly day) ? day : null
                }, http.RequestAborted);

            return await AfterAsync(http, customerId, response.Succeeded, response.Error, "status", get, brand, errors);
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

    // Owner, 2026-10-29 (design 4.4): the company's customer list for those who hold it; for staff with the lookup only, a search for one customer.
    private static async Task<IResult> ListOrLookUpAsync(HttpContext http, string? search, int page, ListOperationsCustomersUseCase list, LookUpOperationsCustomersUseCase lookUp,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        ListOperationsCustomersResponse listed = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsCustomersRequest { Search = search, Page = page }, http.RequestAborted);

        if (listed.Error?.Code != OperationsAccessErrors.PermissionRequired.Code)
        {
            return await ListAsync(http, search, page, null, null, list, brand, errors, listed);
        }

        // Nothing typed yet: no lookup is recorded, only the permission is checked.
        LookUpOperationsCustomersResponse found = await lookUp.ExecuteAsync(OperationsCurrentUser.Context(http), new LookUpOperationsCustomersRequest { Text = search }, http.RequestAborted);

        if (found.Error?.Code == OperationsAccessErrors.PermissionRequired.Code)
        {
            return await ListAsync(http, search, page, null, null, list, brand, errors, listed);
        }

        if (found.Succeeded && found.Customers.Count == 1)
        {
            return Results.Redirect("/staff/customers/" + found.Customers[0].CustomerId);
        }

        return Page<CustomerLookupPage>(http, new
        {
            Company = await CompanyAsync(http, brand),
            Search = search,
            Customers = found.Customers,
            Error = errors.Describe(found.Error)
        });
    }

    private static async Task<IResult> ListAsync(HttpContext http, string? search, int page, Error? error, string? info, ListOperationsCustomersUseCase list,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, ListOperationsCustomersResponse? listed = null)
    {
        OperationsAuthorizationContext context = OperationsCurrentUser.Context(http);
        ListOperationsCustomersResponse response = listed ?? await list.ExecuteAsync(context, new ListOperationsCustomersRequest { Search = search, Page = page }, http.RequestAborted);

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
            // The status panel needs it (it was never passed, so no one could suspend or close a customer).
            CanManage = response.CanManage,
            Shipments = response.Shipments,
            CanSeeShipments = response.CanSeeShipments,
            CanBan = response.CanBan,
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
