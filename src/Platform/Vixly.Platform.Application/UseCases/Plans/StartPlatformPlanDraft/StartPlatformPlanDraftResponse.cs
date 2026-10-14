using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.StartPlatformPlanDraft;

// =========================================================
// FILE: StartPlatformPlanDraftResponse.cs
//
// PURPOSE:
// Starts a new draft from the current version of a plan (D-S2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartPlatformPlanDraftResponse
{
    private StartPlatformPlanDraftResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static StartPlatformPlanDraftResponse Success()
    {
        return new StartPlatformPlanDraftResponse { Succeeded = true };
    }

    public static StartPlatformPlanDraftResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartPlatformPlanDraftResponse { Error = error };
    }
}
