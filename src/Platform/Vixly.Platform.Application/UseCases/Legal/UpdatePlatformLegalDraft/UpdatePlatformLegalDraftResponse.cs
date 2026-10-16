using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.UpdatePlatformLegalDraft;

// =========================================================
// FILE: UpdatePlatformLegalDraftResponse.cs
//
// PURPOSE:
// Changes a draft: version label, acceptance rule, effective date and included versions.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformLegalDraftResponse
{
    private UpdatePlatformLegalDraftResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformLegalDraftResponse Success()
    {
        return new UpdatePlatformLegalDraftResponse { Succeeded = true };
    }

    public static UpdatePlatformLegalDraftResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformLegalDraftResponse { Error = error };
    }
}
