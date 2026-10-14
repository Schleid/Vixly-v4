using Microsoft.Extensions.Localization;
using Vixly.Contracts.Platform.Registration;
using Vixly.Platform.Resources;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Api.Endpoints;

// =========================================================
// FILE: PlatformApiErrors.cs
//
// PURPOSE:
// Turns error codes into API responses.
//
// RESPONSIBILITIES:
// - Describe an error in the caller's language
// - Pick the HTTP status for a code
//
// IMPORTANT NOTES:
// - 401 for a missing or unknown access token; 429 for the link
//   throttle; 400 for every other refusal
// - The body is always { code, message }
// =========================================================
public sealed class PlatformApiErrors
{
    private readonly IStringLocalizer<PlatformText> _platform;

    private readonly IStringLocalizer<SharedText> _shared;

    public PlatformApiErrors(IStringLocalizer<PlatformText> platform, IStringLocalizer<SharedText> shared)
    {
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _shared = shared ?? throw new ArgumentNullException(nameof(shared));
    }

    public PlatformApiError Describe(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PlatformApiError(error.Code, VixlyText.Describe(_platform, _shared, error));
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
}
