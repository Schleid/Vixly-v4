using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.UpdateOperationsSiteDraft;

// =========================================================
// FILE: UpdateOperationsSiteDraftResponse.cs
//
// PURPOSE:
// Saves the Owner changes to the draft site: settings and the texts of one language (Slice 1B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdateOperationsSiteDraftResponse
{
    private UpdateOperationsSiteDraftResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdateOperationsSiteDraftResponse Success()
    {
        return new UpdateOperationsSiteDraftResponse { Succeeded = true };
    }

    public static UpdateOperationsSiteDraftResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdateOperationsSiteDraftResponse { Error = error };
    }
}
