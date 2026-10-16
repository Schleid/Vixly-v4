using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Collections.CheckPlatformTenantCollections;

// =========================================================
// FILE: CheckPlatformTenantCollectionsResponse.cs
//
// PURPOSE:
// Looks at one tenant again now, as the clock would (D-C8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CheckPlatformTenantCollectionsResponse
{
    private CheckPlatformTenantCollectionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CheckPlatformTenantCollectionsResponse Success()
    {
        return new CheckPlatformTenantCollectionsResponse { Succeeded = true };
    }

    public static CheckPlatformTenantCollectionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CheckPlatformTenantCollectionsResponse { Error = error };
    }
}
