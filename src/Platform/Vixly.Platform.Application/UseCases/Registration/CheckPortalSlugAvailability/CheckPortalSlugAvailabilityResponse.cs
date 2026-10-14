using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registration.CheckPortalSlugAvailability;

// =========================================================
// FILE: CheckPortalSlugAvailabilityResponse.cs
//
// PURPOSE:
// Result of CheckPortalSlugAvailability.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CheckPortalSlugAvailabilityResponse
{
    private CheckPortalSlugAvailabilityResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string Slug { get; private init; } = string.Empty;

    public static CheckPortalSlugAvailabilityResponse Success(string slug)
    {
        return new CheckPortalSlugAvailabilityResponse { Succeeded = true, Slug = slug };
    }

    public static CheckPortalSlugAvailabilityResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CheckPortalSlugAvailabilityResponse { Error = error };
    }
}
