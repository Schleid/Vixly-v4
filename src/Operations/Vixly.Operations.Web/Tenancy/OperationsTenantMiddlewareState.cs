using Vixly.Operations.Application.Tenancy;

namespace Vixly.Operations.Web.Tenancy;

// =========================================================
// FILE: OperationsTenantMiddlewareState.cs
//
// PURPOSE:
// Questions about the tenant state of the current request.
//
// RESPONSIBILITIES:
// - Say whether the tenant is live
//
// IMPORTANT NOTES:
// - Reads what OperationsTenantMiddleware found
// =========================================================
public static class OperationsTenantMiddlewareState
{
    public static bool IsLive(HttpContext http) => OperationsTenantMiddleware.Host(http)?.State == OperationsTenantState.Live;
}
