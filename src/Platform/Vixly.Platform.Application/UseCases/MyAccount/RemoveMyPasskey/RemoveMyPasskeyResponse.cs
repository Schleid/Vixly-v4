using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.RemoveMyPasskey;

// =========================================================
// FILE: RemoveMyPasskeyResponse.cs
//
// PURPOSE:
// Result of RemoveMyPasskey.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveMyPasskeyResponse
{
    private RemoveMyPasskeyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveMyPasskeyResponse Success()
    {
        return new RemoveMyPasskeyResponse { Succeeded = true };
    }

    public static RemoveMyPasskeyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveMyPasskeyResponse { Error = error };
    }
}
