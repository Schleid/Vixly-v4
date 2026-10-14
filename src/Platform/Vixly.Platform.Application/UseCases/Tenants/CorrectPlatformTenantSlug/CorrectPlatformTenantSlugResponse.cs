using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.CorrectPlatformTenantSlug;

// =========================================================
// FILE: CorrectPlatformTenantSlugResponse.cs
//
// PURPOSE:
// Result of CorrectPlatformTenantSlug.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CorrectPlatformTenantSlugResponse
{
    private CorrectPlatformTenantSlugResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CorrectPlatformTenantSlugResponse Success()
    {
        return new CorrectPlatformTenantSlugResponse { Succeeded = true };
    }

    public static CorrectPlatformTenantSlugResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CorrectPlatformTenantSlugResponse { Error = error };
    }
}
