using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Account.ChangeMyOperationsLanguage;

// =========================================================
// FILE: ChangeMyOperationsLanguageResponse.cs
//
// PURPOSE:
// Saves the language of my pages and emails (Slice 1E).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeMyOperationsLanguageResponse
{
    private ChangeMyOperationsLanguageResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeMyOperationsLanguageResponse Success()
    {
        return new ChangeMyOperationsLanguageResponse { Succeeded = true };
    }

    public static ChangeMyOperationsLanguageResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeMyOperationsLanguageResponse { Error = error };
    }
}
