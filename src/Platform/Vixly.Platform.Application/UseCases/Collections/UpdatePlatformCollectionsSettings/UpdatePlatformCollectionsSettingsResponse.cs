using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Collections.UpdatePlatformCollectionsSettings;

// =========================================================
// FILE: UpdatePlatformCollectionsSettingsResponse.cs
//
// PURPOSE:
// Changes the collections timeline, block levels and extension limits (D-C12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformCollectionsSettingsResponse
{
    private UpdatePlatformCollectionsSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformCollectionsSettingsResponse Success()
    {
        return new UpdatePlatformCollectionsSettingsResponse { Succeeded = true };
    }

    public static UpdatePlatformCollectionsSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformCollectionsSettingsResponse { Error = error };
    }
}
