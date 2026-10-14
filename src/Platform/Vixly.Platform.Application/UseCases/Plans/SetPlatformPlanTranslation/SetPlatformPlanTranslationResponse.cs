using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.SetPlatformPlanTranslation;

// =========================================================
// FILE: SetPlatformPlanTranslationResponse.cs
//
// PURPOSE:
// Sets the name and description of a plan in one language.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetPlatformPlanTranslationResponse
{
    private SetPlatformPlanTranslationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetPlatformPlanTranslationResponse Success()
    {
        return new SetPlatformPlanTranslationResponse { Succeeded = true };
    }

    public static SetPlatformPlanTranslationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetPlatformPlanTranslationResponse { Error = error };
    }
}
