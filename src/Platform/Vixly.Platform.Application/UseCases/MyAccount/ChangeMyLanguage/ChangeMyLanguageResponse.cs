using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.ChangeMyLanguage;

// =========================================================
// FILE: ChangeMyLanguageResponse.cs
//
// PURPOSE:
// Result of ChangeMyLanguage.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeMyLanguageResponse
{
    private ChangeMyLanguageResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeMyLanguageResponse Success()
    {
        return new ChangeMyLanguageResponse { Succeeded = true };
    }

    public static ChangeMyLanguageResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeMyLanguageResponse { Error = error };
    }
}
