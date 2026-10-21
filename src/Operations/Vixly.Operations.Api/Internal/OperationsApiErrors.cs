using Microsoft.AspNetCore.Http;
using Vixly.Contracts.Operations.Common;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Api.Internal;

// =========================================================
// FILE: OperationsApiErrors.cs
//
// PURPOSE:
// Writes the one error shape of Operations.Api (S-9).
//
// RESPONSIBILITIES:
// - Map an error code to its HTTP status
// - Answer with code, arguments and correlation id
//
// IMPORTANT NOTES:
// - Never exception text
// - The correlation id is also the X-Correlation-Id header
// =========================================================
public static class OperationsApiErrors
{
    public const string CorrelationHeader = "X-Correlation-Id";

    public static IResult Problem(HttpContext http, Error error)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(error);

        int status = error.Code switch
        {
            _ when error.Equals(OperationsInternalErrors.TokenInvalid) => StatusCodes.Status401Unauthorized,
            _ when error.Equals(OperationsInternalErrors.ScopeNotAllowed) || error.Equals(OperationsInternalErrors.TenantRequired) => StatusCodes.Status403Forbidden,
            _ when error.Equals(OperationsCommonErrors.NotFound) => StatusCodes.Status404NotFound,
            "ops.customers.session_required" => StatusCodes.Status401Unauthorized,
            "ops.receiving.parcel_not_found" or "ops.receiving.pre_alert_not_found" or "ops.shipments.not_found" => StatusCodes.Status404NotFound,
            _ when error.Equals(OperationsCommonErrors.PlatformUnavailable) => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };

        Dictionary<string, string>? arguments = error.Arguments.Count == 0
            ? null
            : error.Arguments.ToDictionary(item => item.Key, item => Convert.ToString(item.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, StringComparer.Ordinal);

        return Results.Json(new OperationsApiErrorDto(error.Code, arguments, http.TraceIdentifier), statusCode: status);
    }
}
