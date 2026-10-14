using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.SetPlatformTenantTags;

// =========================================================
// FILE: SetPlatformTenantTagsResponse.cs
//
// PURPOSE:
// Result of SetPlatformTenantTags.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetPlatformTenantTagsResponse
{
    private SetPlatformTenantTagsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetPlatformTenantTagsResponse Success()
    {
        return new SetPlatformTenantTagsResponse { Succeeded = true };
    }

    public static SetPlatformTenantTagsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetPlatformTenantTagsResponse { Error = error };
    }
}
