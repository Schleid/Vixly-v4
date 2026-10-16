using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Collections.GetPlatformTenantCollections;

// =========================================================
// FILE: GetPlatformTenantCollectionsResponse.cs
//
// PURPOSE:
// Reads the Collections section of a tenant page: the open case and overdue invoices.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformTenantCollectionsResponse
{
    private GetPlatformTenantCollectionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantCollectionsSummary? Summary { get; private init; }

    public static GetPlatformTenantCollectionsResponse Success(PlatformTenantCollectionsSummary? summary)
    {
        return new GetPlatformTenantCollectionsResponse { Succeeded = true, Summary = summary };
    }

    public static GetPlatformTenantCollectionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformTenantCollectionsResponse { Error = error };
    }
}
