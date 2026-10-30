using Vixly.Operations.Application.Tenancy;
using Vixly.Operations.Web.Tenancy;

namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsAccessGate.cs
//
// PURPOSE:
// Refuses changes while the company is read-only, payment is required or there is no subscription (Slice 1C; Platform Slices 6 and 8).
//
// RESPONSIBILITIES:
// - Let reading pages through; send any change on a staff page to the "read-only" page
// - Keep sign-in, sign-out, password reset and the Terms acceptance open
//
// IMPORTANT NOTES:
// - Payment required and no subscription are treated like read-only until the billing pages arrive (1E); the tenant's customers are never affected
// - The banner (AccessBanner) says why on every staff page
// =========================================================
public sealed class OperationsAccessGate
{
    private static readonly string[] OpenPaths = { "/staff/sign-in", "/staff/sign-out", "/staff/forgot-password", "/staff/reset-password", "/staff/legal/accept", "/staff/confirm", "/staff/billing" };

    // While a payment is due, the Owner and Administrators reach only these pages (design section 3).
    private static readonly string[] PaymentPaths = { "/staff/billing", "/staff/limited", "/staff/legal", "/staff/account", "/staff/confirm", "/staff/sign-in", "/staff/sign-out" };

    private readonly RequestDelegate _next;

    public OperationsAccessGate(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);

        PathString path = http.Request.Path;

        if (path.StartsWithSegments("/staff")
            && http.User.Identity?.IsAuthenticated == true
            && OperationsTenantMiddleware.Host(http) is { State: OperationsTenantState.Live, Access: OperationsTenantAccess.PaymentRequired }
            && !PaymentPaths.Any(open => path.StartsWithSegments(open)))
        {
            bool canPay = http.User.FindFirst(OperationsClaimTypes.Owner)?.Value == "1" || http.User.FindFirst(OperationsClaimTypes.Administrator)?.Value == "1";
            http.Response.Redirect(canPay ? "/staff/billing" : "/staff/limited", false, false);
            http.Response.StatusCode = StatusCodes.Status303SeeOther;
            return;
        }

        if (HttpMethods.IsPost(http.Request.Method)
            && path.StartsWithSegments("/staff")
            && !OpenPaths.Any(open => path.StartsWithSegments(open))
            && OperationsTenantMiddleware.Host(http) is { State: OperationsTenantState.Live } host
            && host.Access != OperationsTenantAccess.Full)
        {
            http.Response.Redirect("/staff/limited", false, false);
            http.Response.StatusCode = StatusCodes.Status303SeeOther;
            return;
        }

        await _next(http);
    }
}
