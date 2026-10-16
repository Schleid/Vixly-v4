using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Vixly.Contracts.Platform.Common;
using Vixly.Platform.Resources;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Api.Endpoints;

// =========================================================
// FILE: PlatformApiErrors.cs
//
// PURPOSE:
// Turns error codes into API responses (Slices 1 to 9).
//
// RESPONSIBILITIES:
// - Describe an error in the caller's language with its arguments and a correlation id
// - Pick the HTTP status for a code, for public and internal routes
//
// IMPORTANT NOTES:
// - Public routes: 401 for a missing or unknown access token, 429 for the link throttle, 400 otherwise (unchanged)
// - Internal routes: 401 token, 403 role, scope or step-up, 404 not found, 409 conflict, 429 throttles, 400 otherwise
// - The body is always { code, message, arguments, correlationId } (D-K7)
// =========================================================
public sealed class PlatformApiErrors
{
    private readonly IStringLocalizer<PlatformText> _platform;

    private readonly IStringLocalizer<SharedText> _shared;

    private readonly IHttpContextAccessor _http;

    public PlatformApiErrors(IStringLocalizer<PlatformText> platform, IStringLocalizer<SharedText> shared, IHttpContextAccessor http)
    {
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _shared = shared ?? throw new ArgumentNullException(nameof(shared));
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public static string CorrelationId(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        return Activity.Current?.TraceId.ToHexString() ?? http.TraceIdentifier;
    }

    public PlatformApiError Describe(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PlatformApiError(
            error.Code,
            VixlyText.Describe(_platform, _shared, error),
            error.Arguments.Count == 0
                ? null
                : error.Arguments.ToDictionary(item => item.Key, item => Convert.ToString(item.Value, CultureInfo.InvariantCulture) ?? string.Empty, StringComparer.Ordinal),
            _http.HttpContext is { } http ? CorrelationId(http) : null);
    }

    public IResult Problem(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        int status = error.Code switch
        {
            "platform.registrations.access_hash_invalid" => StatusCodes.Status401Unauthorized,
            "platform.registrations.resend_too_soon" => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Json(Describe(error), statusCode: status);
    }

    public IResult InternalProblem(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return Results.Json(Describe(error), statusCode: InternalStatus(error.Code));
    }

    public static int InternalStatus(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return code switch
        {
            "platform.internal.token_invalid" => StatusCodes.Status401Unauthorized,
            "platform.internal.scope_not_allowed" or "platform.internal.tenant_required" or "platform.common.role_not_allowed"
                or "platform.common.step_up_required" or "platform.legal.owner_only" => StatusCodes.Status403Forbidden,
            "platform.common.concurrency_conflict" => StatusCodes.Status409Conflict,
            "platform.licensing.too_many_links" => StatusCodes.Status429TooManyRequests,
            _ when code.EndsWith("_not_found", StringComparison.Ordinal) || code == "platform.common.not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };
    }
}
