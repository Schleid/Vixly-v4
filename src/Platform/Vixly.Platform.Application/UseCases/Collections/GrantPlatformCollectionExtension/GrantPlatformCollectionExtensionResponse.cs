using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Collections.GrantPlatformCollectionExtension;

// =========================================================
// FILE: GrantPlatformCollectionExtensionResponse.cs
//
// PURPOSE:
// Gives a tenant more time: reminders pause and a block is lifted until the date (D-C7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GrantPlatformCollectionExtensionResponse
{
    private GrantPlatformCollectionExtensionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static GrantPlatformCollectionExtensionResponse Success()
    {
        return new GrantPlatformCollectionExtensionResponse { Succeeded = true };
    }

    public static GrantPlatformCollectionExtensionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GrantPlatformCollectionExtensionResponse { Error = error };
    }
}
