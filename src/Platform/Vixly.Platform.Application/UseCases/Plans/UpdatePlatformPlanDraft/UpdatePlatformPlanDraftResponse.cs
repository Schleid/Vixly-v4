using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.UpdatePlatformPlanDraft;

// =========================================================
// FILE: UpdatePlatformPlanDraftResponse.cs
//
// PURPOSE:
// Edits the draft version of a plan (D-S2, D-S7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformPlanDraftResponse
{
    private UpdatePlatformPlanDraftResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformPlanDraftResponse Success()
    {
        return new UpdatePlatformPlanDraftResponse { Succeeded = true };
    }

    public static UpdatePlatformPlanDraftResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformPlanDraftResponse { Error = error };
    }
}
