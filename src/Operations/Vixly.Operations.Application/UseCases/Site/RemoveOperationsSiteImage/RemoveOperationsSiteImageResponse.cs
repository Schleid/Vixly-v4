using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.RemoveOperationsSiteImage;

// =========================================================
// FILE: RemoveOperationsSiteImageResponse.cs
//
// PURPOSE:
// Removes a logo, favicon or hero image from the draft site (Slice 1B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveOperationsSiteImageResponse
{
    private RemoveOperationsSiteImageResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveOperationsSiteImageResponse Success()
    {
        return new RemoveOperationsSiteImageResponse { Succeeded = true };
    }

    public static RemoveOperationsSiteImageResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveOperationsSiteImageResponse { Error = error };
    }
}
