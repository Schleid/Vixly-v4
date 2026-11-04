using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.RemoveFromOperationsBundleList;

// =========================================================
// FILE: RemoveFromOperationsBundleListResponse.cs
//
// PURPOSE:
// Take a line off a bundle's list before it is scanned (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveFromOperationsBundleListResponse
{
    private RemoveFromOperationsBundleListResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveFromOperationsBundleListResponse Success()
    {
        return new RemoveFromOperationsBundleListResponse { Succeeded = true };
    }

    public static RemoveFromOperationsBundleListResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveFromOperationsBundleListResponse { Error = error };
    }
}
