using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.UpdatePlatformZelleAccount;

// =========================================================
// FILE: UpdatePlatformZelleAccountResponse.cs
//
// PURPOSE:
// Saves Vixly's Zelle details and turns Zelle at checkout on or off.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformZelleAccountResponse
{
    private UpdatePlatformZelleAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformZelleAccountResponse Success()
    {
        return new UpdatePlatformZelleAccountResponse { Succeeded = true };
    }

    public static UpdatePlatformZelleAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformZelleAccountResponse { Error = error };
    }
}
