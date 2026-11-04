using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.AddToOperationsBundleList;

// =========================================================
// FILE: AddToOperationsBundleListResponse.cs
//
// PURPOSE:
// Put parcels and closed repack boxes on a bundle's list from the computer, to be confirmed by a scan at the station (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AddToOperationsBundleListResponse
{
    private AddToOperationsBundleListResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static AddToOperationsBundleListResponse Success()
    {
        return new AddToOperationsBundleListResponse { Succeeded = true };
    }

    public static AddToOperationsBundleListResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AddToOperationsBundleListResponse { Error = error };
    }
}
