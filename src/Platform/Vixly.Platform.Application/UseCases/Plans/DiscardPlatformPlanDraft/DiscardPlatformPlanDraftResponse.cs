using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.DiscardPlatformPlanDraft;

// =========================================================
// FILE: DiscardPlatformPlanDraftResponse.cs
//
// PURPOSE:
// Discards the draft of a plan that has a published version.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DiscardPlatformPlanDraftResponse
{
    private DiscardPlatformPlanDraftResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DiscardPlatformPlanDraftResponse Success()
    {
        return new DiscardPlatformPlanDraftResponse { Succeeded = true };
    }

    public static DiscardPlatformPlanDraftResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DiscardPlatformPlanDraftResponse { Error = error };
    }
}
