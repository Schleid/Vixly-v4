using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.ChangeMyPassword;

// =========================================================
// FILE: ChangeMyPasswordResponse.cs
//
// PURPOSE:
// Result of ChangeMyPassword.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeMyPasswordResponse
{
    private ChangeMyPasswordResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int OtherSessionsEnded { get; private init; }

    public static ChangeMyPasswordResponse Success(int otherSessionsEnded)
    {
        return new ChangeMyPasswordResponse { Succeeded = true, OtherSessionsEnded = otherSessionsEnded };
    }

    public static ChangeMyPasswordResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeMyPasswordResponse { Error = error };
    }
}
